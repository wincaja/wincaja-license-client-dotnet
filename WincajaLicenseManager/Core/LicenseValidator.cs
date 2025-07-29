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
2h3Lz8xQZ2hxFL5h3Y2j8z7xQZYRxQ5Qa2X3w6xZgY2xZgY3Lz8xQZ2hxFLYR2h
3Y2j8z7xQZYRxQ5Qa2X3w6xZgY2xZgY3Lz8xQZ2hxFL5h3Y2j8z7xQZYRxQZgY3
Lz8xQZ2hxFL5h3Y2j8wIDAQAB
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
                    var onlineResult = PerformOnlineValidation(storedLicense);
                    if (onlineResult != null)
                    {
                        // Update stored license with server response
                        if (onlineResult.Success && onlineResult.Data != null)
                        {
                            storedLicense.LastValidation = DateTime.UtcNow;
                            if (onlineResult.Data.ExpiresAt.HasValue)
                            {
                                storedLicense.ExpiresAt = onlineResult.Data.ExpiresAt;
                            }
                            _storage.SaveLicense(storedLicense);

                            // Update status
                            status.LastValidation = storedLicense.LastValidation;
                            status.RequiresOnlineValidation = false;
                            status.GraceDaysRemaining = _gracePeriodDays;
                        }
                        else if (!onlineResult.Success)
                        {
                            // Server rejected the license
                            status.IsValid = false;
                            status.Status = "invalid";
                            status.Error = onlineResult.Error ?? "License validation failed";
                            return status;
                        }
                    }
                    // If online validation fails due to network issues, continue with grace period
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

        private ValidationResponse PerformOnlineValidation(StoredLicense license)
        {
            try
            {
                using (var apiClient = new ApiClient())
                {
                    var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                    return apiClient.ValidateLicense(license.LicenseKey, license.ActivationId, hardwareInfo);
                }
            }
            catch
            {
                // Network or API errors are handled gracefully
                return null;
            }
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
                        HardwareFingerprint = fingerprint, // Store our calculated fingerprint
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