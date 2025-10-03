using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using WincajaLicenseManager.Core;
using WincajaLicenseManager.Models;

namespace WincajaLicenseManager
{
    [ComVisible(true)]
    [Guid("F8A3C9D5-2B6E-4D7F-A3E8-9D7F6B4C8E3B")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("WincajaLicenseManager.LicenseAPI")]
    public class WincajaLicenseManagerImpl : IWincajaLicenseManager
    {
        private readonly LicenseValidator _validator;
        private readonly HardwareFingerprinter _fingerprinter;
        private string _apiBaseUrl;

        public WincajaLicenseManagerImpl()
        {
            _validator = new LicenseValidator();
            _fingerprinter = new HardwareFingerprinter();
        }

        public string ActivateLicense(string licenseKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "License key is required"
                    });
                }

                string error;
                var success = _validator.ActivateLicense(licenseKey, out error);

                if (success)
                {
                    // Get the current status after activation
                    var status = _validator.ValidateLicense(false);
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = "License activated successfully",
                        status = status.Status,
                        expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                        daysUntilExpiration = status.DaysUntilExpiration
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = error ?? "Activation failed"
                    });
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Activation error: {ex.Message}"
                });
            }
        }

        public string ValidateLicense()
        {
            try
            {
                var status = _validator.ValidateLicense(true);

                var result = new
                {
                    success = status.IsValid,
                    status = status.Status,
                    licenseKey = status.LicenseKey,
                    expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                    daysUntilExpiration = status.DaysUntilExpiration,
                    lastValidation = status.LastValidation.ToString("yyyy-MM-dd HH:mm:ss"),
                    graceDaysRemaining = status.GraceDaysRemaining,
                    requiresOnlineValidation = status.RequiresOnlineValidation,
                    error = status.Error
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    status = "error",
                    error = $"Validation error: {ex.Message}"
                });
            }
        }

        public string ValidateLicenseForceOnline()
        {
            try
            {
                Console.WriteLine("[DEBUG] ValidateLicenseForceOnline() - START");
                // Force an immediate online validation regardless of grace period
                var status = _validator.ForceOnlineValidation();
                Console.WriteLine($"[DEBUG] ValidateLicenseForceOnline() - Status received: IsValid={status.IsValid}, ActivationLimitExceeded={status.ActivationLimitExceeded}");

                var result = new
                {
                    success = status.IsValid,
                    status = status.Status,
                    licenseKey = status.LicenseKey,
                    expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                    daysUntilExpiration = status.DaysUntilExpiration,
                    lastValidation = status.LastValidation.ToString("yyyy-MM-dd HH:mm:ss"),
                    graceDaysRemaining = status.GraceDaysRemaining,
                    requiresOnlineValidation = false,
                    features = status.features,
                    productversion = status.ProductVersion,
                    error = status.Error,
                    
                    // Activation limit information
                    activationLimitExceeded = status.ActivationLimitExceeded,
                    currentActivations = status.CurrentActivations,
                    activationLimit = status.ActivationLimit,
                    licenseStatusFromServer = status.LicenseStatusFromServer
                };

                var jsonResult = JsonConvert.SerializeObject(result);
                Console.WriteLine($"[DEBUG] ValidateLicenseForceOnline() - JSON result: {jsonResult}");
                return jsonResult;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    status = "error",
                    error = $"Validation error: {ex.Message}"
                });
            }
        }

        public string GetLicenseStatus()
        {
            try
            {
                // Validate without online check for quick status
                var status = _validator.ValidateLicense(false);

                var result = new
                {
                    success = true,
                    isValid = status.IsValid,
                    status = status.Status,
                    licenseKey = status.LicenseKey,
                    expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                    daysUntilExpiration = status.DaysUntilExpiration,
                    lastValidation = status.LastValidation.ToString("yyyy-MM-dd HH:mm:ss"),
                    graceDaysRemaining = status.GraceDaysRemaining,
                    requiresOnlineValidation = status.RequiresOnlineValidation
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Status check error: {ex.Message}"
                });
            }
        }

        public string GetHardwareFingerprint()
        {
            try
            {
                var fingerprint = _fingerprinter.GetHardwareFingerprint();
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();

                var result = new
                {
                    success = true,
                    fingerprint = fingerprint,
                    hardware = hardwareInfo
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Hardware fingerprint error: {ex.Message}"
                });
            }
        }

        public string DeactivateLicense()
        {
            return DeactivateLicense(false);
        }

        public string DeactivateLicense(bool forceLocalOnly)
        {
            try
            {
                var result = _validator.DeactivateLicense(forceLocalOnly);

                var response = new
                {
                    success = result.Success,
                    message = result.Message ?? (result.Success ? "License deactivated successfully" : result.Error),
                    error = result.Error,
                    warning = result.Warning,
                    deactivationType = result.DeactivationType.ToString(),
                    canForceLocal = result.CanForceLocal,
                    remainingActivations = result.RemainingActivations,
                    serverUpdated = result.DeactivationType == DeactivationType.ServerAndLocal,
                    localOnly = result.DeactivationType == DeactivationType.LocalOnly
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Deactivation error: {ex.Message}"
                });
            }
        }

        public void SetApiBaseUrl(string baseUrl)
        {
            _apiBaseUrl = baseUrl;
            // Note: Would need to modify ApiClient to accept custom base URL
            // For now, this is a placeholder for future implementation
        }
    }
}