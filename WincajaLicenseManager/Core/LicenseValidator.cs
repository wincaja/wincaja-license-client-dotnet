using System;
using System.Security.Cryptography;
using System.Text;
using WincajaLicenseManager.Models;

namespace WincajaLicenseManager.Core
{
    internal class LicenseValidator
    {
        private readonly SecureStorage _storage;
        private readonly HardwareFingerprinter _fingerprinter;
        private readonly int _gracePeriodDays;

        // RSA public key for signature verification (if needed in future)
        private const string PublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwKU3QZBKfEw+A5p6nAsO
qerCOWDFhvqBRuqgPvSKKDIiU7I0n5nzWqS2TpzQkPL0p2dWvZ5rFCy1v3f2h8Lx
WZgvXZG5xOc6jL6w0xQ7LnQ5qe5Y1M5xQ9vQ2Z5qJ7hxFL2hzU2j3tYm8z7xQZYR
xQ5Qa2X3w6xZgY2xZgY3Lz8xQZ2hxFL5h3Y2j8z7xQZYRxQ5Qa2X3w6xZgY2xQZ
2h3Lz8xQZ2hxFL5h3Y2j8z7xQZYRxQZgY3Lz8xQZ2hxFL5h3Y2j8wIDAQAB
-----END PUBLIC KEY-----";

        public LicenseValidator(int gracePeriodDays = 7)
        {
            _storage = new SecureStorage();
            _fingerprinter = new HardwareFingerprinter();
            _gracePeriodDays = gracePeriodDays;
        }

        public LicenseStatus ValidateLicense(bool performOnlineCheck = true)
        {
            try
            {
                // Load stored license
                var storedLicense = _storage.LoadLicense<StoredLicense>();
                if (storedLicense == null)
                {
                    return new LicenseStatus
                    {
                        IsValid = false,
                        Status = "not_activated",
                        Error = "No license found. Please activate your license."
                    };
                }

                // Basic validation
                var status = new LicenseStatus
                {
                    LicenseKey = MaskLicenseKey(storedLicense.LicenseKey),
                    ExpiresAt = storedLicense.ExpiresAt,
                    LastValidation = storedLicense.LastValidation
                };

                // Check expiration
                if (storedLicense.ExpiresAt.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if (now > storedLicense.ExpiresAt.Value)
                    {
                        status.IsValid = false;
                        status.Status = "expired";
                        status.Error = "License has expired";
                        status.DaysUntilExpiration = 0;
                        return status;
                    }

                    status.DaysUntilExpiration = (int)(storedLicense.ExpiresAt.Value - now).TotalDays;
                }
                else
                {
                    status.DaysUntilExpiration = int.MaxValue; // Perpetual license
                }

                // Check hardware fingerprint
                var currentFingerprint = _fingerprinter.GetHardwareFingerprint();
                if (!CompareFingerprints(storedLicense.HardwareFingerprint, currentFingerprint))
                {
                    status.IsValid = false;
                    status.Status = "hardware_mismatch";
                    status.Error = "Hardware fingerprint mismatch. License is bound to different hardware.";
                    return status;
                }

                // Check grace period for online validation
                var daysSinceLastValidation = (DateTime.UtcNow - storedLicense.LastValidation).Days;
                status.GraceDaysRemaining = Math.Max(0, _gracePeriodDays - daysSinceLastValidation);
                status.RequiresOnlineValidation = daysSinceLastValidation >= _gracePeriodDays;

                // Perform online validation if needed and requested
                if (performOnlineCheck && status.RequiresOnlineValidation)
                {
                    var onlineResult = PerformOnlineValidationHardware(storedLicense);
                    if (onlineResult != null)
                    {
                        // NUEVA LÓGICA INTELIGENTE: Manejar respuestas basadas en HasLicense y Valid
                        if (onlineResult.HasLicense && onlineResult.Valid && onlineResult.License != null)
                        {
                            // Licencia disponible y válida
                            storedLicense.LastValidation = DateTime.UtcNow;
                            if (onlineResult.License.ExpiresAt.HasValue)
                            {
                                storedLicense.ExpiresAt = onlineResult.License.ExpiresAt;
                            }
                            _storage.SaveLicense(storedLicense);

                            status.LastValidation = storedLicense.LastValidation;
                            status.RequiresOnlineValidation = false;
                            status.GraceDaysRemaining = _gracePeriodDays;
                        }
                        else if (onlineResult.HasLicense && !onlineResult.Valid)
                        {
                            // Licencia activada pero no disponible - actualizar timestamp pero mantener estado
                            storedLicense.LastValidation = DateTime.UtcNow;
                            _storage.SaveLicense(storedLicense);

                            status.LastValidation = storedLicense.LastValidation;
                            status.RequiresOnlineValidation = false;
                            status.GraceDaysRemaining = _gracePeriodDays;
                            
                            // Determinar motivo específico
                            if (onlineResult.Validation?.ActivationLimitExceeded == true)
                            {
                                status.IsValid = false;
                                status.Status = "activation_limit_exceeded";
                                status.Error = "Esta licencia ya fue activada en otra máquina.";
                            }
                            else if (onlineResult.Ssl?.Required == true && onlineResult.Ssl?.Validation?.Valid == false)
                            {
                                status.IsValid = false;
                                status.Status = "ssl_validation_failed";
                                status.Error = GetSslErrorMessage(onlineResult);
                            }
                            else
                            {
                                status.IsValid = false;
                                status.Status = "license_not_available";
                                status.Error = "La licencia no está disponible para uso en esta máquina.";
                            }
                            return status;
                        }
                        else if (!onlineResult.HasLicense)
                        {
                            // No tiene licencia
                            status.IsValid = false;
                            status.Status = "not_activated";
                            status.Error = "No se encontró licencia activa.";
                            status.RequiresOnlineValidation = false;
                            status.GraceDaysRemaining = 0;
                            return status;
                        }
                        // Handle legacy format
                        else if (onlineResult.Success && onlineResult.Data != null)
                        {
                            storedLicense.LastValidation = DateTime.UtcNow;
                            if (onlineResult.Data.ExpiresAt.HasValue)
                            {
                                storedLicense.ExpiresAt = onlineResult.Data.ExpiresAt;
                            }
                            _storage.SaveLicense(storedLicense);

                            status.LastValidation = storedLicense.LastValidation;
                            status.RequiresOnlineValidation = false;
                            status.GraceDaysRemaining = _gracePeriodDays;
                        }
                        else if (!onlineResult.Valid && !onlineResult.Success)
                        {
                            // Check if this is a revoked license
                            bool isRevoked = IsRevocationResponse(onlineResult);
                            if (isRevoked)
                            {
                                // CRITICAL: Clear cache immediately for revoked licenses
                                _storage.DeleteLicense();
                                
                                status.IsValid = false;
                                status.Status = "revoked";
                                status.Error = "License has been revoked by administrator";
                                status.RequiresOnlineValidation = false;
                                status.GraceDaysRemaining = 0;
                                return status;
                            }
                            
                            // For other validation failures, return invalid but keep cache
                            status.IsValid = false;
                            status.Status = "invalid";
                            status.Error = onlineResult.Error ?? "License validation failed";
                            return status;
                        }
                    }
                }

                // License is valid
                status.IsValid = true;
                status.Status = "active";
                return status;
            }
            catch (Exception ex)
            {
                return new LicenseStatus
                {
                    IsValid = false,
                    Status = "error",
                    Error = $"License validation error: {ex.Message}"
                };
            }
        }

        public LicenseStatus ForceOnlineValidation()
        {
            try
            {
                Console.WriteLine("[DEBUG] ForceOnlineValidation() - START");
                var storedLicense = _storage.LoadLicense<StoredLicense>();
                if (storedLicense == null)
                {
                    Console.WriteLine("[DEBUG] ForceOnlineValidation() - No stored license found");
                    return new LicenseStatus
                    {
                        IsValid = false,
                        Status = "not_activated",
                        Error = "No license found. Please activate your license."
                    };
                }

                Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Stored license found: {MaskLicenseKey(storedLicense.LicenseKey)}");
                Console.WriteLine($"[DEBUG] ForceOnlineValidation() - About to call server with activationId: {storedLicense.ActivationId}");

                using (var apiClient = new ApiClient())
                {
                    var fp = !string.IsNullOrWhiteSpace(storedLicense.ServerHardwareFingerprint) ? storedLicense.ServerHardwareFingerprint : storedLicense.HardwareFingerprint;
                    Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Making HTTP call to server...");
                    Console.WriteLine($"[INFO] ForceOnlineValidation() - SSL no es enviado (API v1.1: ssl.used maneja reactivaciones)");
                    // ACTUALIZADO: No enviar SSL (obsoleto con API v1.1)
                    var serverResult = apiClient.ValidateLicenseHardware(storedLicense.LicenseKey, fp, storedLicense.ActivationId);
                    Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Server response received: {serverResult != null}");

                    var status = new LicenseStatus
                    {
                        LicenseKey = MaskLicenseKey(storedLicense.LicenseKey),
                        ExpiresAt = storedLicense.ExpiresAt,
                        LastValidation = storedLicense.LastValidation
                        
                    };

                    if (serverResult == null)
                    {
                        Console.WriteLine("[DEBUG] ForceOnlineValidation() - Server result is NULL");
                        status.IsValid = false;
                        status.Status = "network_error";
                        status.Error = "No response from server";
                        return status;
                    }

                    Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Server result: Valid={serverResult.Valid}, Success={serverResult.Success}");
                    if (serverResult.Validation != null)
                    {
                        Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Validation info: ActivationLimitExceeded={serverResult.Validation.ActivationLimitExceeded}, CurrentActivations={serverResult.Validation.CurrentActivations}, ActivationLimit={serverResult.Validation.ActivationLimit}");
                    }

                    // NUEVA LÓGICA INTELIGENTE: Manejar respuestas basadas en HasLicense y Valid
                    Console.WriteLine($"[DEBUG] ForceOnlineValidation() - HasLicense={serverResult.HasLicense}, Valid={serverResult.Valid}");


                    if (serverResult.Data == null && serverResult.License == null && serverResult.Ssl == null)
                    {
                        status.IsValid = false;
                        status.Status = "invalid";
                        status.Error = serverResult.Error ?? "License is not valid";
                        status.RequiresOnlineValidation = false;
                        status.GraceDaysRemaining = _gracePeriodDays;
                        status.DaysUntilExpiration = 0;

                        return status;
                    }


                    // Caso 1: Licencia disponible y válida (puede usar normalmente)
                    if (serverResult.HasLicense && serverResult.Valid && serverResult.License != null)
                    {
                        Console.WriteLine("[DEBUG] ForceOnlineValidation() - Licencia disponible y válida");
                        
                        // Update stored license timestamps and expiration
                        storedLicense.LastValidation = DateTime.UtcNow;
                        if (serverResult.License.ExpiresAt.HasValue)
                        {
                            storedLicense.ExpiresAt = serverResult.License.ExpiresAt;
                        }
                        _storage.SaveLicense(storedLicense);

                        status.LastValidation = storedLicense.LastValidation;
                        status.GraceDaysRemaining = _gracePeriodDays;
                        status.RequiresOnlineValidation = false;
                      
                        // Use server validity
                        status.IsValid = true; // Licencia disponible para usar
                        status.Status = "active";
                        status.Error = null;
                        status.DaysUntilExpiration = storedLicense.ExpiresAt.HasValue
                            ? Math.Max(0, (int)(storedLicense.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                            : int.MaxValue;

                        status.ProductVersion = serverResult.License.ProductVersion;
                        status.OrganizationName = serverResult.License.OrganizationName;
                        status.features = serverResult.License.Features;
                        status.ExpiresAt = serverResult.License.ExpiresAt;

                        status.Suggestion = serverResult.Suggestion;
                        status.RequestId = serverResult.RequestId;
                        status.Diagnostic = serverResult.Diagnostic;


                        return status;
                    }
                    
                    // Caso 2: Licencia activada pero no disponible (ya fue usada)
                    else if (serverResult.HasLicense && !serverResult.Valid)
                    {
                        Console.WriteLine("[DEBUG] ForceOnlineValidation() - Licencia activada pero no disponible");
                        
                        // Update stored license timestamps
                        storedLicense.LastValidation = DateTime.UtcNow;
                        _storage.SaveLicense(storedLicense);

                        status.LastValidation = storedLicense.LastValidation;
                        status.GraceDaysRemaining = _gracePeriodDays;
                        status.RequiresOnlineValidation = false;
                        
                        // Determinar el motivo específico
                        if (serverResult.Validation?.ActivationLimitExceeded == true)
                        {
                            status.IsValid = false;
                            status.Status = "activation_limit_exceeded";
                            status.Error = "Esta licencia ya fue activada en otra máquina. Límite de activaciones alcanzado.";
                        }
                        else if (serverResult.Ssl?.Required == true && serverResult.Ssl?.Validation?.Valid == false)
                        {
                            status.IsValid = false;
                            status.Status = "ssl_validation_failed";
                            status.Error = GetSslErrorMessage(serverResult);
                        }
                        else
                        {
                            status.IsValid = false;
                            status.Status = "license_not_available";
                            status.Error = "La licencia no está disponible para uso en esta máquina.";
                        }
                        
                        // Extraer información de activación
                        if (serverResult.Validation != null)
                        {
                            status.ActivationLimitExceeded = serverResult.Validation.ActivationLimitExceeded;
                            status.CurrentActivations = serverResult.Validation.CurrentActivations;
                            status.ActivationLimit = serverResult.Validation.ActivationLimit;
                        }
                        
                        // Extraer información de licencia
                        if (serverResult.License != null)
                        {
                            status.LicenseStatusFromServer = serverResult.License.Status;
                            status.ProductVersion = serverResult.License.ProductVersion;
                            status.features = serverResult.License.Features;

                            status.Suggestion = serverResult.Suggestion;
                            status.RequestId = serverResult.RequestId;
                            status.Diagnostic = serverResult.Diagnostic;


                            if (serverResult.License.ExpiresAt.HasValue)
                            {
                                status.ExpiresAt = serverResult.License.ExpiresAt;
                                status.DaysUntilExpiration = Math.Max(0, (int)(serverResult.License.ExpiresAt.Value - DateTime.UtcNow).TotalDays);
                            }
                        }
                        
                        return status;
                    }
                    
                    // Caso 3: No tiene licencia (necesita activar)
                    else if (!serverResult.HasLicense)
                    {
                        Console.WriteLine("[DEBUG] ForceOnlineValidation() - No tiene licencia");
                        
                        status.IsValid = false;
                        status.Status = "not_activated";
                        status.Error = "No se encontró licencia activa. Por favor active su licencia.";
                        status.RequiresOnlineValidation = false;
                        status.GraceDaysRemaining = 0;
                        status.DaysUntilExpiration = 0;

                        return status;
                    }
                    // Handle legacy format for backward compatibility
                    else if (serverResult.Success && serverResult.Data != null)
                    {
                        storedLicense.LastValidation = DateTime.UtcNow;
                        if (serverResult.Data.ExpiresAt.HasValue)
                        {
                            storedLicense.ExpiresAt = serverResult.Data.ExpiresAt;
                        }
                        _storage.SaveLicense(storedLicense);

                        status.LastValidation = storedLicense.LastValidation;
                        status.GraceDaysRemaining = _gracePeriodDays;
                        status.RequiresOnlineValidation = false;
                        status.IsValid = serverResult.Data.Valid;
                        status.Status = serverResult.Data.Status ?? (serverResult.Data.Valid ? "active" : "invalid");
                        status.Error = serverResult.Data.Valid ? null : (serverResult.Data.Message ?? serverResult.Error);
                        status.DaysUntilExpiration = storedLicense.ExpiresAt.HasValue
                            ? Math.Max(0, (int)(storedLicense.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                            : int.MaxValue;

                        status.Suggestion = serverResult.Suggestion;
                        status.RequestId = serverResult.RequestId;
                        status.Diagnostic = serverResult.Diagnostic;

                        return status;
                    }

                    // Server indicated invalid or error
                    bool isRevoked = IsRevocationResponse(serverResult);
                    if (isRevoked)
                    {
                        // CRITICAL: Clear cache immediately for revoked licenses
                        _storage.DeleteLicense();
                        
                        status.IsValid = false;
                        status.Status = "revoked";
                        status.Error = "License has been revoked by administrator";
                        status.RequiresOnlineValidation = false;
                        status.GraceDaysRemaining = 0;
                        status.DaysUntilExpiration = 0;
                        return status;
                    }
                    
                    // For other validation failures - extract activation limit info
                    Console.WriteLine("[DEBUG] ForceOnlineValidation() - Processing validation failure (valid=false)");
                    status.IsValid = false;
                    status.Status = "invalid";
                    status.Error = serverResult.Error ?? "License is not valid";
                    status.RequiresOnlineValidation = false;
                    status.GraceDaysRemaining = _gracePeriodDays;
                    status.DaysUntilExpiration = 0;
                    
                    // Extract activation limit information from server response
                    if (serverResult.Validation != null)
                    {
                        status.ActivationLimitExceeded = serverResult.Validation.ActivationLimitExceeded;
                        status.CurrentActivations = serverResult.Validation.CurrentActivations;
                        status.ActivationLimit = serverResult.Validation.ActivationLimit;
                    }
                    
                    // Extract license status from server response
                    if (serverResult.License != null)
                    {
                        status.LicenseStatusFromServer = serverResult.License.Status;
                        status.ProductVersion = serverResult.License.ProductVersion;
                        status.features = serverResult.License.Features;

                        status.Suggestion = serverResult.Suggestion;
                        status.RequestId = serverResult.RequestId;
                        status.Diagnostic = serverResult.Diagnostic;

                        // Update expiration if available
                        if (serverResult.License.ExpiresAt.HasValue)
                        {
                            status.ExpiresAt = serverResult.License.ExpiresAt;
                            status.DaysUntilExpiration = Math.Max(0, (int)(serverResult.License.ExpiresAt.Value - DateTime.UtcNow).TotalDays);
                        }
                    }
                    
                    Console.WriteLine($"[DEBUG] ForceOnlineValidation() - Returning status: IsValid={status.IsValid}, ActivationLimitExceeded={status.ActivationLimitExceeded}");
                    return status;
                }
            }
            catch (Exception ex)
            {
                return new LicenseStatus
                {
                    IsValid = false,
                    Status = "error",
                    Error = $"Force validation error: {ex.Message}"
                };
            }
        }

        private ValidationResponse PerformOnlineValidationHardware(StoredLicense license)
        {
            try
            {
                using (var apiClient = new ApiClient())
                {
                    // Prefer server fingerprint if available, fall back to local
                    var fp = !string.IsNullOrWhiteSpace(license.ServerHardwareFingerprint) ? license.ServerHardwareFingerprint : license.HardwareFingerprint;
                    Console.WriteLine($"[INFO] PerformOnlineValidationHardware - SSL no es enviado (API v1.1: ssl.used maneja reactivaciones)");
                    // ACTUALIZADO: No enviar SSL (obsoleto con API v1.1)
                    return apiClient.ValidateLicenseHardware(license.LicenseKey, fp, license.ActivationId);
                }
            }
            catch
            {
                return null;
            }
        }

        private bool IsRevocationResponse(ValidationResponse response)
        {
            if (response == null) return false;
            
            // Check for explicit revocation indicators
            if (response.License != null && response.License.Status != null)
            {
                var status = response.License.Status.ToLowerInvariant();
                if (status == "revoked" || status == "suspended" || status == "disabled")
                    return true;
            }
            
            // Check legacy format
            if (response.Data != null && response.Data.Status != null)
            {
                var status = response.Data.Status.ToLowerInvariant();
                if (status == "revoked" || status == "suspended" || status == "disabled")
                    return true;
            }
            
            // Check error message for revocation keywords
            if (!string.IsNullOrEmpty(response.Error))
            {
                var errorLower = response.Error.ToLowerInvariant();
                if (errorLower.Contains("revoked") || 
                    errorLower.Contains("suspended") || 
                    errorLower.Contains("disabled") ||
                    errorLower.Contains("deactivated") ||
                    errorLower.Contains("unauthorized") && errorLower.Contains("license"))
                    return true;
            }
            
            // Check for UNAUTHORIZED status code with license-related message
            if (response.StatusCode == 401 && !string.IsNullOrEmpty(response.Error))
            {
                var errorLower = response.Error.ToLowerInvariant();
                if (errorLower.Contains("license"))
                    return true;
            }
            
            return false;
        }

        private bool CompareFingerprints(string stored, string current)
        {
            // For now, exact match. In future, could implement tolerance for minor changes
            return string.Equals(stored, current, StringComparison.OrdinalIgnoreCase);
        }

        private string MaskLicenseKey(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey) || licenseKey.Length < 8)
                return "****";

            var visible = 4; // Show first 4 and last 4 characters
            var prefix = licenseKey.Substring(0, visible);
            var suffix = licenseKey.Substring(licenseKey.Length - visible);
            var masked = new string('*', Math.Max(4, licenseKey.Length - (2 * visible)));

            return $"{prefix}{masked}{suffix}";
        }

        // NUEVO: Método para verificar requisitos SSL de una licencia
        // Consulta el servidor para determinar si es primera activación
        public SslRequirementInfo CheckSslRequirement(string licenseKey, out string error)
        {
            error = null;
            var info = new SslRequirementInfo();

            try
            {
                using (var apiClient = new ApiClient())
                {
                    // Validar sin hardware check para obtener info SSL
                    Console.WriteLine($"[DEBUG] CheckSslRequirement - Consultando estado SSL para {MaskLicenseKey(licenseKey)}");
                    var response = apiClient.ValidateLicense(licenseKey);

                    if (response?.Ssl != null)
                    {
                        info.IsRequired = response.Ssl.Required;
                        info.IsFirstActivation = !response.Ssl.Used; // ← CLAVE: ssl.used = false significa primera activación
                        info.IsMigrated = response.Ssl.MigratedFromLegacy;
                        info.LegacySslNumber = response.Ssl.LegacySslNumber;

                        if (info.IsFirstActivation && info.IsRequired)
                        {
                            info.Message = "Esta licencia requiere SSL para primera activación. " +
                                          "Por favor proporcione el número SSL que aparece en su documento de licencia.";
                            Console.WriteLine($"[INFO] Primera activación detectada - SSL requerido");
                        }
                        else if (info.IsRequired && !info.IsFirstActivation)
                        {
                            info.Message = "Esta licencia ya fue activada previamente. SSL no es necesario para reactivar.";
                            Console.WriteLine($"[INFO] Reactivación detectada - SSL opcional");
                        }
                        else
                        {
                            info.Message = "Esta licencia no requiere SSL.";
                            Console.WriteLine($"[INFO] Licencia nueva - SSL no requerido");
                        }
                    }
                    else
                    {
                        // Licencia nueva sin SSL
                        info.IsRequired = false;
                        info.IsFirstActivation = false;
                        info.IsMigrated = false;
                        info.Message = "Esta licencia no requiere SSL.";
                    }

                    return info;
                }
            }
            catch (Exception ex)
            {
                error = $"Error al verificar requisitos SSL: {ex.Message}";
                Console.WriteLine($"[ERROR] CheckSslRequirement failed: {ex.Message}");
                return info;
            }
        }

        public bool ActivateLicense(string licenseKey, out ValidationResponse vresponse, string sslNumber = null)
        {
            vresponse = new ValidationResponse();
            vresponse.Error = null;

            try
            {
                // 🔍 DEBUGGING: Punto de entrada
                Console.WriteLine($"[SELF-DEBUG] ActivateLicense INICIADO para: {MaskLicenseKey(licenseKey)}");
                Console.WriteLine($"[SELF-DEBUG] SSL proporcionado: {!string.IsNullOrEmpty(sslNumber)}");
                
                // NUEVO: Verificar requisitos SSL antes de activar
                Console.WriteLine($"[DEBUG] ActivateLicense - Verificando requisitos SSL...");
                var sslInfo = CheckSslRequirement(licenseKey, out var checkError);
                
                // 🔍 DEBUGGING: Resultado de SSL check
                Console.WriteLine($"[SELF-DEBUG] CheckSslRequirement completado:");
                Console.WriteLine($"[SELF-DEBUG] - checkError: {checkError ?? "null"}");
                Console.WriteLine($"[SELF-DEBUG] - sslInfo.IsRequired: {sslInfo?.IsRequired}");
                Console.WriteLine($"[SELF-DEBUG] - sslInfo.IsFirstActivation: {sslInfo?.IsFirstActivation}");
                
                if (!string.IsNullOrEmpty(checkError))
                {
                    vresponse.Error = checkError;
                    Console.WriteLine($"[SELF-DEBUG] 🚨 BLOQUEADO EN PUNTO #1: Error SSL check");
                    Console.WriteLine($"[ERROR] ActivateLicense - Error al verificar SSL: {checkError}");
                    return false;
                }

                // 🔍 DEBUGGING: Verificar condición de bloqueo SSL
                bool wouldBlockSsl = sslInfo.IsFirstActivation && sslInfo.IsRequired && string.IsNullOrEmpty(sslNumber);
                Console.WriteLine($"[SELF-DEBUG] Evaluando bloqueo SSL:");
                Console.WriteLine($"[SELF-DEBUG] - IsFirstActivation: {sslInfo.IsFirstActivation}");
                Console.WriteLine($"[SELF-DEBUG] - IsRequired: {sslInfo.IsRequired}");
                Console.WriteLine($"[SELF-DEBUG] - sslNumber vacío: {string.IsNullOrEmpty(sslNumber)}");
                Console.WriteLine($"[SELF-DEBUG] - ¿Bloquearía?: {wouldBlockSsl}");

                // NUEVO: Si es primera activación de licencia migrada y NO se proporcionó SSL
                if (sslInfo.IsFirstActivation && sslInfo.IsRequired && string.IsNullOrEmpty(sslNumber))
                {
                    vresponse.Error = "SSL_REQUIRED_FOR_FIRST_ACTIVATION: Esta licencia requiere su número SSL para la primera activación. " +
                            "Por favor proporcione el número SSL que aparece en su documento de licencia.";
                    Console.WriteLine($"[SELF-DEBUG] 🚨 BLOQUEADO EN PUNTO #2: SSL requerido pero no proporcionado");
                    Console.WriteLine($"[ERROR] Primera activación - SSL requerido pero no proporcionado");
                    vresponse.Message = sslInfo.Message;
                    return false;
                }

                // NUEVO: Si NO es primera activación y se proporcionó SSL, advertir que no es necesario
                if (!sslInfo.IsFirstActivation && !string.IsNullOrEmpty(sslNumber))
                {
                    Console.WriteLine($"[INFO] SSL proporcionado pero no es necesario (licencia ya activada previamente). Continuando...");
                    // No es error, el servidor lo manejará correctamente
                }

                // 🔍 DEBUGGING: Llegamos al punto de envío
                Console.WriteLine($"[SELF-DEBUG] ✅ PASÓ TODAS LAS VALIDACIONES - Preparando envío al servidor");
                
                // Get hardware info
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                var fingerprint = _fingerprinter.GetHardwareFingerprint();

                Console.WriteLine($"[DEBUG] ActivateLicense - Enviando request de activación{(string.IsNullOrEmpty(sslNumber) ? " sin SSL" : " con SSL")}");
                Console.WriteLine($"[SELF-DEBUG] Hardware fingerprint: {fingerprint?.Substring(0, Math.Min(8, fingerprint.Length ?? 0))}...");

                // Call activation API
                using (var apiClient = new ApiClient())
                {
                    Console.WriteLine($"[SELF-DEBUG] 🌐 LLAMANDO AL SERVIDOR AHORA...");
                    var response = apiClient.ActivateLicense(licenseKey, hardwareInfo, sslNumber);
                    Console.WriteLine($"[SELF-DEBUG] 🌐 RESPUESTA RECIBIDA: Success={response?.Success}, Error={response?.Error}");

                    if (!response.Success)
                    {
                        // Manejar errores específicos de SSL (nueva lógica API v1.1)
                        if (response.Error?.Contains("SSL_REQUIRED") == true)
                        {
                            vresponse.Error = "SSL_REQUIRED: Esta licencia requiere un número SSL para primera activación. " +
                                    "Por favor proporcione el número SSL que aparece en su documento.";
                        }
                        else if (response.Error?.Contains("SSL_MISMATCH") == true)
                        {
                            vresponse.Error = "SSL_MISMATCH: El número SSL proporcionado no coincide con el registrado. " +
                                    "Verifique el SSL en su documento de licencia.";
                        }
                        else if (response.Error?.Contains("ACTIVATION_LIMIT_EXCEEDED") == true || 
                                 response.Error?.Contains("Activation limit reached") == true)
                        {
                            vresponse.Error = "ACTIVATION_LIMIT_EXCEEDED: Esta licencia ya alcanzó el límite de activaciones permitidas. " +
                                    "Debe desactivar una activación existente primero.";
                        }
                        else
                        {
                            vresponse.Error = response.Error ?? "Activation failed";
                        }

                        vresponse.Suggestion = response.Suggestion;
                        vresponse.RequestId = response.RequestId;
                        vresponse.Message = response.Message;
                        vresponse.Diagnostic = response.Diagnostic;

                        Console.WriteLine($"[ERROR] Activación fallida: {vresponse.Error}");
                        return false;
                    }

                    // NUEVO: Informar al usuario si fue primera activación exitosa con SSL
                    if (response.Ssl?.Used == true && response.Ssl?.FirstActivation != null)
                    {
                        Console.WriteLine("✅ Primera activación exitosa con SSL.");
                        Console.WriteLine("   Ya no necesitará el número SSL para futuras activaciones en otras máquinas.");
                    }
                    else if (response.Ssl?.Used == true)
                    {
                        Console.WriteLine("✅ Reactivación exitosa (SSL no fue necesario).");
                    }
                    else
                    {
                        Console.WriteLine("✅ Activación exitosa.");
                    }

                    vresponse.Suggestion = response.Suggestion;
                    vresponse.RequestId = response.RequestId;
                    vresponse.Message = response.Message;
                    vresponse.Diagnostic = response.Diagnostic;

                    // Store the activated license
                    var storedLicense = new StoredLicense
                    {
                        LicenseKey = licenseKey,
                        ActivationId = response.ActivationId,
                        HardwareFingerprint = fingerprint,
                        ServerHardwareFingerprint = string.IsNullOrWhiteSpace(response.HardwareFingerprint) ? fingerprint : response.HardwareFingerprint,
                        ActivatedAt = DateTime.TryParse(response.ActivatedAt, out var activatedAt) ? activatedAt : DateTime.UtcNow,
                        ExpiresAt = null,
                        LastValidation = DateTime.UtcNow,
                        RemainingActivations = response.RemainingActivations,
                        LicenseInfo = null
                        // NOTA: SslNumber ya no se guarda (obsoleto con API v1.1)
                    };

                    _storage.SaveLicense(storedLicense);
                    return true;
                }
            }
            catch (Exception ex)
            {
                vresponse.Error = $"Activation error: {ex.Message}";
                Console.WriteLine($"[ERROR] ActivateLicense - Exception: {ex.Message}");
                return false;
            }
        }

        public DeactivationResult DeactivateLicense(bool forceLocalOnly = false)
        {
            // Get the currently stored license to extract activation details
            var storedLicense = _storage.LoadLicense<StoredLicense>();
            if (storedLicense == null)
            {
                return new DeactivationResult
                {
                    Success = false,
                    Error = "No license found to deactivate",
                    DeactivationType = DeactivationType.None
                };
            }

            if (!forceLocalOnly)
            {
                // Try server-side deactivation first
                try
                {
                    using (var apiClient = new ApiClient())
                    {
                        var serverResponse = apiClient.DeactivateLicense(
                            storedLicense.LicenseKey,
                            storedLicense.ActivationId,
                            "User requested deactivation via client"
                        );

                        if (serverResponse.Success)
                        {
                            // Server deactivation successful, now remove local file
                            var localDeleted = _storage.DeleteLicense();
                            
                            return new DeactivationResult
                            {
                                Success = true,
                                Message = serverResponse.Message ?? "License deactivated successfully on server and locally",
                                DeactivationType = DeactivationType.ServerAndLocal,
                                RemainingActivations = serverResponse.RemainingActivations,
                                ServerResponse = serverResponse
                            };
                        }
                        else
                        {
                            // Server deactivation failed - offer local-only option
                            return new DeactivationResult
                            {
                                Success = false,
                                Error = $"Server deactivation failed: {serverResponse.Error}",
                                DeactivationType = DeactivationType.None,
                                CanForceLocal = true,
                                ServerResponse = serverResponse
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Network or other error - offer local-only deactivation
                    return new DeactivationResult
                    {
                        Success = false,
                        Error = $"Cannot connect to server: {ex.Message}",
                        DeactivationType = DeactivationType.None,
                        CanForceLocal = true
                    };
                }
            }
            else
            {
                // Force local-only deactivation (last resort)
                var localDeleted = _storage.DeleteLicense();
                
                return new DeactivationResult
                {
                    Success = localDeleted,
                    Message = localDeleted 
                        ? "License deactivated locally only. The server still shows this activation as active. Contact support if needed."
                        : "Failed to deactivate license locally",
                    DeactivationType = localDeleted ? DeactivationType.LocalOnly : DeactivationType.None,
                    Warning = "Local-only deactivation: The license slot on the server is still consumed."
                };
            }
        }

        // NUEVOS MÉTODOS PARA MANEJO DE SSL
        public bool LicenseRequiresSsl(ValidationResponse response)
        {
            return response?.Ssl?.Required == true;
        }

        public bool ValidateSsl(string sslNumber, ValidationResponse response)
        {
            if (!response.Ssl.Required) return true;
            if (string.IsNullOrEmpty(sslNumber)) return false;
            return response.Ssl.Validation?.Valid == true;
        }

        public string GetSslErrorMessage(ValidationResponse response)
        {
            if (response?.Ssl?.Validation?.Error == "SSL_REQUIRED_NOT_PROVIDED")
                return "Esta licencia migrada requiere un número SSL para activación";
            if (response?.Ssl?.Validation?.Error == "SSL_MISMATCH")
                return "El número SSL proporcionado no coincide con el SSL de la licencia";
            return response?.Ssl?.Validation?.Message ?? "Error SSL desconocido";
        }

        public bool IsSslError(ValidationResponse response)
        {
            return response?.Ssl?.Validation?.Error != null && 
                   (response.Ssl.Validation.Error == "SSL_REQUIRED_NOT_PROVIDED" || 
                    response.Ssl.Validation.Error == "SSL_MISMATCH");
        }

        public bool IsSslUsed(ValidationResponse response)
        {
            return response?.Ssl?.Used == true;
        }

        public DateTime? GetSslFirstActivation(ValidationResponse response)
        {
            return response?.Ssl?.FirstActivation;
        }

        public StoredLicense GetStoredLicense()
        {
            return _storage.LoadLicense<StoredLicense>();
        }
    }
}