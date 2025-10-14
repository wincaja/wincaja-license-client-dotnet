using System;
using System.Collections.Generic;

namespace WincajaLicenseManager.Models
{
    // ============================================================================
    // IMPORTANTE: ACLARACIÓN SOBRE "SSL" EN ESTE CÓDIGO
    // ============================================================================
    // En este proyecto, "SSL" NO se refiere al protocolo de seguridad (Secure Sockets Layer)
    // 
    // "SSL" aquí significa "SL" = Security Lock / Serial License Number
    //   - Es el identificador del chip físico de seguridad del sistema legacy
    //   - Campo en BD histórica: licNoChip
    // 
    // Ejemplos de números SL del sistema legacy:
    //   - "SL11A13197"
    //   - "SL24A04200"
    //   - "SL25A05030"
    //
    // Este número se usa ÚNICAMENTE para validar la primera activación 
    // de licencias migradas del sistema anterior.
    // ============================================================================


    public class ActivationRequest
    {
        public string LicenseKey { get; set; }
        public Dictionary<string, object> HardwareInfo { get; set; }
        public string BindingMode { get; set; } = "flexible";
        // NUEVO: Número SL (Serial License) para licencias migradas del sistema legacy
        // Formato: "SL" + código alfanumérico (ej: "SL11A13197", "SL24A04200")
        public string SslNumber { get; set; }
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
        // NUEVO: Información del número SL (Serial License) de la respuesta
        public SslInfo Ssl { get; set; }
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
        // NUEVO: Número SL (Serial License) para licencias migradas del sistema legacy
        // Formato: "SL" + código alfanumérico (ej: "SL11A13197", "SL24A04200")
        public string SslNumber { get; set; }
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
        // NUEVO: Información del número SL (Serial License) de la respuesta
        public SslInfo Ssl { get; set; }
        
        // NUEVO: Campo calculado para indicar si la PC tiene licencia
        public bool HasLicense { get; set; }
    }

    public class LicenseData
    {
        public string LicenseId { get; set; }
        public string LicenseKey { get; set; }
        public string ClientEmail { get; set; }
        public string OrganizationName { get; set; }
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
        
        // OBSOLETO: Con API v1.1, el número SL ya no necesita guardarse localmente
        // El número SL (Serial License) solo es requerido en la primera activación, después es opcional
        // NOTA: "SSL" aquí significa "SL" (Serial License Number), NO el protocolo de seguridad
        [Obsolete("El número SL no necesita guardarse con la nueva API v1.1. El servidor maneja el estado ssl.used.")]
        public string SslNumber { get; set; }
    }

    public class LicenseStatus
    {
        public bool IsValid { get; set; }
        public string Status { get; set; }
        public string LicenseKey { get; set; }
        public string OrganizationName { get; set; }
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

    // NUEVAS CLASES PARA SOPORTAR LICENCIAS MIGRADAS
    // NOTA: "SSL" se refiere a "SL" (Serial License Number) del sistema legacy
    // NO confundir con SSL (Secure Sockets Layer) - aquí significa "Serial License"
    public class SslInfo
    {
        public bool Required { get; set; }
        public bool Used { get; set; }
        public DateTime? FirstActivation { get; set; }
        public bool MigratedFromLegacy { get; set; }
        public string LegacySslNumber { get; set; }  // Número SL del sistema legacy (ej: "SL11A13197")
        public SslValidation Validation { get; set; }
    }

    public class SslValidation
    {
        public bool Valid { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    // NUEVO: Información sobre requisitos de número SL para una licencia
    // NOTA: "SSL" = "SL" (Serial License Number), NO el protocolo de seguridad
    public class SslRequirementInfo
    {
        public bool IsRequired { get; set; }
        public bool IsFirstActivation { get; set; }
        public bool IsMigrated { get; set; }
        public string Message { get; set; }
        public string LegacySslNumber { get; set; }  // Número SL del sistema legacy (ej: "SL11A13197")
    }
}