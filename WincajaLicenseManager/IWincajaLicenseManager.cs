using System;
using System.Runtime.InteropServices;

namespace WincajaLicenseManager
{
    [ComVisible(true)]
    [Guid("E7F2D8C4-9A5B-4F3E-B1D6-7C9E5A3F8B2A")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IWincajaLicenseManager
    {
        /// <summary>
        /// Activates a new license key. Returns a JSON string with the result.
        /// </summary>
        /// <param name="licenseKey">The license key to activate</param>
        /// <param name="sslNumber">Optional SSL number for migrated licenses</param>
        /// <returns>JSON string with activation result</returns>
        string ActivateLicense(string licenseKey, string sslNumber = null);

        /// <summary>
        /// Validates the existing license. The core function called on app startup.
        /// Returns a JSON string with the validation status.
        /// </summary>
        /// <returns>JSON string with validation result</returns>
        string ValidateLicense();

        /// <summary>
        /// Forces an immediate online validation, ignoring the grace period.
        /// Useful to reflect server-side changes (e.g., deactivation) right away.
        /// </summary>
        /// <returns>JSON string with validation result</returns>
        string ValidateLicenseForceOnline();

        /// <summary>
        /// Gets the current license status without performing a full validation.
        /// </summary>
        /// <returns>JSON string with license status</returns>
        string GetLicenseStatus();

        /// <summary>
        /// Gets the hardware fingerprint for the current machine.
        /// Useful for debugging/support purposes.
        /// </summary>
        /// <returns>JSON string with hardware fingerprint</returns>
        string GetHardwareFingerprint();

        /// <summary>
        /// Deactivates and removes the current license.
        /// </summary>
        /// <returns>JSON string with deactivation result</returns>
        string DeactivateLicense();

        /// <summary>
        /// Deactivates and removes the current license with hybrid approach.
        /// </summary>
        /// <param name="forceLocalOnly">If true, skip server deactivation and only deactivate locally</param>
        /// <returns>JSON string with deactivation result</returns>
        string DeactivateLicense(bool forceLocalOnly);

        /// <summary>
        /// Sets the API base URL (optional, defaults to http://localhost:5174/api/licenses)
        /// </summary>
        /// <param name="baseUrl">The base URL for the license API</param>
        void SetApiBaseUrl(string baseUrl);

        /// <summary>
        /// Checks if a license requires SSL validation
        /// </summary>
        /// <param name="licenseKey">The license key to check</param>
        /// <returns>JSON string indicating if SSL is required</returns>
        string CheckSslRequirement(string licenseKey);

        /// <summary>
        /// Validates a license with SSL number
        /// </summary>
        /// <param name="licenseKey">The license key to validate</param>
        /// <param name="sslNumber">The SSL number for migrated licenses</param>
        /// <returns>JSON string with validation result including SSL info</returns>
        string ValidateLicenseWithSsl(string licenseKey, string sslNumber);

        /// <summary>
        /// Gets SSL information for the current license
        /// </summary>
        /// <returns>JSON string with SSL information</returns>
        string GetSslInfo();
    }
}