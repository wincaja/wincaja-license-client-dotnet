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

        public string ActivateLicense(string licenseKey, string sslNumber = null)
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

                string error="";
                //04/11/2025

                //var success = _validator.ActivateLicense(licenseKey, out error, sslNumber);
                var success = _validator.ActivateLicense(licenseKey, out ValidationResponse response, sslNumber);
                if (success)
                {
                    // Get the current status after activation
                    var status = _validator.ValidateLicense(false);
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        //message = "License activated successfully",
                        message = response.Message?? "License activated successfully",
                        status = status.Status,
                        expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                        daysUntilExpiration = status.DaysUntilExpiration,
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        //error = error ?? "Activation failed"
                        error = response.Error ?? "Activation failed",
                        message= response.Message,
                        suggestion = response.Suggestion,
                        requestid= response.RequestId,
                        Diagnostic = response.Diagnostic,
                       
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
                // Force an immediate online validation regardless of grace period
                var status = _validator.ForceOnlineValidation();

                var result = new
                {
                    success = status.IsValid,
                    status = status.Status,
                    licenseKey = status.LicenseKey,
                    organizationName = status.OrganizationName,
                    expiresAt = status.ExpiresAt?.ToString("yyyy-MM-dd"),
                    daysUntilExpiration = status.DaysUntilExpiration,
                    lastValidation = status.LastValidation.ToString("yyyy-MM-dd HH:mm:ss"),
                    graceDaysRemaining = status.GraceDaysRemaining,
                    requiresOnlineValidation = false,
                    features = status.features,
                    productversion = status.ProductVersion,
                    error = status.Error,
                    requestid= status.RequestId,
                    suggestion=status.Suggestion,
                    diagnostic= status.Diagnostic
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

        public string CheckSslRequirement(string licenseKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "License key is required",
                        sslRequired = false
                    });
                }

                // Get hardware info for validation
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                var fingerprint = _fingerprinter.GetHardwareFingerprint();

                // Call validation API to check SSL requirement
                using (var apiClient = new ApiClient())
                {
                    var response = apiClient.ValidateLicenseHardware(licenseKey, fingerprint, null, null);

                    if (response != null)
                    {
                        var sslRequired = _validator.LicenseRequiresSsl(response);
                        var sslInfo = response.Ssl;

                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            sslRequired = sslRequired,
                            sslInfo = sslInfo != null ? new
                            {
                                required = sslInfo.Required,
                                used = sslInfo.Used,
                                firstActivation = sslInfo.FirstActivation?.ToString("yyyy-MM-dd HH:mm:ss"),
                                migratedFromLegacy = sslInfo.MigratedFromLegacy,
                                legacySslNumber = sslInfo.LegacySslNumber,
                                validation = sslInfo.Validation != null ? new
                                {
                                    valid = sslInfo.Validation.Valid,
                                    message = sslInfo.Validation.Message,
                                    error = sslInfo.Validation.Error
                                } : null
                            } : null,
                            error = response.Error
                        });
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "No response from server",
                            sslRequired = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"SSL check error: {ex.Message}",
                    sslRequired = false
                });
            }
        }

        public string ValidateLicenseWithSsl(string licenseKey, string sslNumber)
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

                // Get hardware info for validation
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                var fingerprint = _fingerprinter.GetHardwareFingerprint();

                // Call validation API with SSL
                using (var apiClient = new ApiClient())
                {
                    var response = apiClient.ValidateLicenseHardware(licenseKey, fingerprint, null, sslNumber);

                    if (response != null)
                    {
                        var isValid = response.Valid && response.Success;
                        var sslInfo = response.Ssl;

                        var result = new
                        {
                            success = isValid,
                            valid = response.Valid,
                            sslRequired = sslInfo?.Required ?? false,
                            sslValid = sslInfo?.Validation?.Valid ?? true,
                            sslInfo = sslInfo != null ? new
                            {
                                required = sslInfo.Required,
                                used = sslInfo.Used,
                                firstActivation = sslInfo.FirstActivation?.ToString("yyyy-MM-dd HH:mm:ss"),
                                migratedFromLegacy = sslInfo.MigratedFromLegacy,
                                legacySslNumber = sslInfo.LegacySslNumber,
                                validation = sslInfo.Validation != null ? new
                                {
                                    valid = sslInfo.Validation.Valid,
                                    message = sslInfo.Validation.Message,
                                    error = sslInfo.Validation.Error
                                } : null
                            } : null,
                            error = response.Error,
                            licenseInfo = response.License != null ? new
                            {
                                licenseKey = response.License.LicenseKey,
                                clientEmail = response.License.ClientEmail,
                                expiresAt = response.License.ExpiresAt?.ToString("yyyy-MM-dd"),
                                status = response.License.Status
                            } : null
                        };

                        return JsonConvert.SerializeObject(result);
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "No response from server"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Validation error: {ex.Message}"
                });
            }
        }

        public string GetSslInfo()
        {
            try
            {
                // Get current stored license
                var storedLicense = _validator.GetStoredLicense();
                if (storedLicense == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No license found",
                        sslInfo = (object)null
                    });
                }

                // Get hardware info for validation
                var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
                var fingerprint = _fingerprinter.GetHardwareFingerprint();

                // Call validation API to get current SSL info
                using (var apiClient = new ApiClient())
                {
                    var response = apiClient.ValidateLicenseHardware(storedLicense.LicenseKey, fingerprint, storedLicense.ActivationId, null);

                    if (response != null && response.Ssl != null)
                    {
                        var sslInfo = response.Ssl;
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            sslInfo = new
                            {
                                required = sslInfo.Required,
                                used = sslInfo.Used,
                                firstActivation = sslInfo.FirstActivation?.ToString("yyyy-MM-dd HH:mm:ss"),
                                migratedFromLegacy = sslInfo.MigratedFromLegacy,
                                legacySslNumber = sslInfo.LegacySslNumber,
                                validation = sslInfo.Validation != null ? new
                                {
                                    valid = sslInfo.Validation.Valid,
                                    message = sslInfo.Validation.Message,
                                    error = sslInfo.Validation.Error
                                } : null
                            }
                        });
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "No SSL information available",
                            sslInfo = (object)null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"SSL info error: {ex.Message}",
                    sslInfo = (object)null
                });
            }
        }
    }
}