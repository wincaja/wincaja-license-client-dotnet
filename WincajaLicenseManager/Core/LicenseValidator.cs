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
                        // Handle new server response format
                        if (onlineResult.Valid && onlineResult.License != null)
                        {
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

                    // Handle new server response format
                    if (serverResult.Valid && serverResult.License != null)
                    {
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
                        status.IsValid = serverResult.Valid;
                        status.Status = serverResult.License.Status ?? "active";
                        status.Error = null;
                        status.DaysUntilExpiration = storedLicense.ExpiresAt.HasValue
                            ? Math.Max(0, (int)(storedLicense.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                            : int.MaxValue;

                        status.ProductVersion = serverResult.License.ProductVersion;
                        status.OrganizationName = serverResult.License.OrganizationName;
                        //features
                        status.features = serverResult.License.Features;
                        status.ExpiresAt = serverResult.License.ExpiresAt;



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

        public bool ActivateLicense(string licenseKey, out string error)
        {
            error = null;

            try
            {
                // Get hardware info
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                var fingerprint = _fingerprinter.GetHardwareFingerprint();

                // Call activation API
                using (var apiClient = new ApiClient())
                {
                    var response = apiClient.ActivateLicense(licenseKey, hardwareInfo);

                    if (!response.Success)
                    {
                        error = response.Error ?? "Activation failed";
                        return false;
                    }

                    // Store the activated license
                    var storedLicense = new StoredLicense
                    {
                        LicenseKey = licenseKey,
                        ActivationId = response.ActivationId,
                        HardwareFingerprint = fingerprint, // local calculated fingerprint
                        ServerHardwareFingerprint = string.IsNullOrWhiteSpace(response.HardwareFingerprint) ? fingerprint : response.HardwareFingerprint,
                        ActivatedAt = DateTime.TryParse(response.ActivatedAt, out var activatedAt) ? activatedAt : DateTime.UtcNow,
                        ExpiresAt = null, // We'll need to get this from a separate validation call if needed
                        LastValidation = DateTime.UtcNow,
                        RemainingActivations = response.RemainingActivations,
                        LicenseInfo = null // We'll need to populate this from a separate call if needed
                    };

                    _storage.SaveLicense(storedLicense);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = $"Activation error: {ex.Message}";
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
    }
}