using System;
using System.Collections.Generic;

namespace WincajaLicenseManager.Models
{
    public class ActivationRequest
    {
        public string LicenseKey { get; set; }
        public Dictionary<string, object> HardwareInfo { get; set; }
        public string BindingMode { get; set; } = "flexible";
    }

    public class ActivationResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        public string ActivationId { get; set; }
        public string HardwareFingerprint { get; set; }
        public string ActivatedAt { get; set; }
        public bool MatchedWithTolerance { get; set; }
        public bool FingerprintUpdated { get; set; }
        public int RemainingActivations { get; set; }
    }

    public class ActivationData
    {
        public string ActivationId { get; set; }
        public string LicenseKey { get; set; }
        public string HardwareFingerprint { get; set; }
        public DateTime ActivatedAt { get; set; }
        public LicenseInfo License { get; set; }
        public int RemainingActivations { get; set; }
    }

    public class LicenseInfo
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string Type { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; }
        public int MaxActivations { get; set; }
    }

    public class ValidationRequest
    {
        public string LicenseKey { get; set; }
        public string ActivationId { get; set; }
        public Dictionary<string, object> HardwareInfo { get; set; }
        public bool IncludeHardwareCheck { get; set; } = false;
    }

    public class ValidationResponse
    {
        public bool Valid { get; set; }
        public LicenseData License { get; set; }
        public ValidationInfo Validation { get; set; }
        public bool Success { get; set; }
        public ValidationData Data { get; set; }
        public string Error { get; set; }
        public int StatusCode { get; set; }
    }

    public class LicenseData
    {
        public string LicenseId { get; set; }
        public string LicenseKey { get; set; }
        public string ClientEmail { get; set; }
        public string ProductId { get; set; }
        public string ProductVersion { get; set; }
        public string LicenseType { get; set; }
        public string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<Feature> Features { get; set; }
        public Dictionary<string, object> Constraints { get; set; }
    }

    public class Feature
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class ValidationInfo
    {
        public bool SignatureValid { get; set; }
        public bool? HardwareValid { get; set; }
        public object HardwareValidationDetails { get; set; }
        public bool ActivationLimitExceeded { get; set; }
        public int CurrentActivations { get; set; }
        public int ActivationLimit { get; set; }
    }

    public class DeactivationResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        public string LicenseKey { get; set; }
        public string ActivationId { get; set; }
        public int RemainingActivations { get; set; }
    }

    public class ValidationData
    {
        public bool Valid { get; set; }
        public string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Message { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class StoredLicense
    {
        public string LicenseKey { get; set; }
        public string ActivationId { get; set; }
        public string HardwareFingerprint { get; set; }
        public string ServerHardwareFingerprint { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime LastValidation { get; set; }
        public int RemainingActivations { get; set; }
        public LicenseInfo LicenseInfo { get; set; }
    }

    public class LicenseStatus
    {
        public bool IsValid { get; set; }
        public string Status { get; set; }
        public string LicenseKey { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int DaysUntilExpiration { get; set; }
        public DateTime LastValidation { get; set; }
        public int GraceDaysRemaining { get; set; }
        public bool RequiresOnlineValidation { get; set; }
        public string Error { get; set; }
        public string ProductVersion { get; set; }
        public List<Feature> features { get; set; }
        
        // Activation limit information
        public bool ActivationLimitExceeded { get; set; }
        public int CurrentActivations { get; set; }
        public int ActivationLimit { get; set; }
        public string LicenseStatusFromServer { get; set; }
    }

    public enum DeactivationType
    {
        None,                // No deactivation occurred
        ServerAndLocal,      // Successfully deactivated on server and locally
        LocalOnly           // Only deactivated locally (server not updated)
    }

    public class DeactivationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        public string Warning { get; set; }
        public DeactivationType DeactivationType { get; set; }
        public bool CanForceLocal { get; set; }
        public int RemainingActivations { get; set; }
        public DeactivationResponse ServerResponse { get; set; }
    }
}