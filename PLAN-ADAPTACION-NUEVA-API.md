# Plan de Adaptación - Nueva Lógica SSL API v1.1

## 📋 **ANÁLISIS COMPLETO**

### **Estado Actual del Cliente .NET**

#### **❌ Problemas Actuales:**

1. **Guarda SSL localmente** en `StoredLicense.SslNumber`

   - Innecesario según nueva lógica
   - Ocupaespacio
   - Usuario no lo necesita después de primera activación

2. **Envía SSL en todas las validaciones**

   - `ForceOnlineValidation()` envía `storedLicense.SslNumber`
   - `PerformOnlineValidationHardware()` envía `license.SslNumber`
   - Innecesario según nueva lógica

3. **No detecta `ssl.used`**

   - No verifica si es primera activación o reactivación
   - Siempre asume que necesita SSL si es licencia migrada

4. **UX no optimizada**
   - Solicita SSL incluso en reactivaciones
   - Usuario debe recordar/buscar SSL innecesariamente

---

### **Nueva Lógica API v1.1**

#### **✅ Comportamiento Esperado:**

```
┌─────────────────────────────────────────────────────────────┐
│ CAMPO CLAVE: ssl.used                                       │
├─────────────────────────────────────────────────────────────┤
│ ssl.used = false → PRIMERA ACTIVACIÓN                       │
│   • SSL es OBLIGATORIO ⚠️                                   │
│   • Usuario DEBE proporcionar SSL                           │
│   • Servidor marca ssl.used = true después                  │
│                                                              │
│ ssl.used = true → REACTIVACIÓN                              │
│   • SSL es OPCIONAL ✅                                      │
│   • Cliente NO debe solicitar SSL                           │
│   • Cliente activa sin SSL                                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 **PLAN DE CAMBIOS**

### **Fase 1: Análisis Pre-Activación**

**Objetivo:** Antes de activar, consultar el estado `ssl.used`

**Nuevo Método en `LicenseValidator`:**

```csharp
public SslRequirementInfo CheckSslRequirement(string licenseKey, out string error)
{
    // 1. Validar licencia sin hardware check
    // 2. Obtener información SSL del servidor
    // 3. Retornar SslRequirementInfo
}

public class SslRequirementInfo
{
    public bool IsRequired { get; set; }        // ssl.required
    public bool IsFirstActivation { get; set; } // !ssl.used
    public bool IsMigrated { get; set; }        // ssl.migratedFromLegacy
    public string Message { get; set; }
}
```

---

### **Fase 2: Activación Inteligente**

**Objetivo:** Activar con o sin SSL según `ssl.used`

**Modificar `ActivateLicense()`:**

```csharp
// OPCIÓN A: Método automático (recomendado)
public bool ActivateLicense(string licenseKey, out string error)
{
    // 1. Verificar ssl.used
    var sslInfo = CheckSslRequirement(licenseKey, out error);

    // 2. Si es primera activación, solicitar SSL
    if (sslInfo.IsFirstActivation && sslInfo.IsRequired)
    {
        error = "SSL_REQUIRED_FOR_FIRST_ACTIVATION";
        return false; // El llamador debe volver a llamar con SSL
    }

    // 3. Activar sin SSL (reactivación o licencia nueva)
    return ActivateLicenseInternal(licenseKey, null, out error);
}

// OPCIÓN B: Método explícito (actual)
public bool ActivateLicense(string licenseKey, out string error, string sslNumber = null)
{
    // Mantener compatibilidad
    // Pero validar que no se envíe SSL innecesario
}
```

---

### **Fase 3: Almacenamiento Simplificado**

**Objetivo:** No guardar SSL localmente (ya no es necesario)

**Modificar `StoredLicense`:**

```csharp
// OPCIÓN A: Eliminar completamente
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
    // ELIMINADO: public string SslNumber { get; set; }
}

// OPCIÓN B: Marcar como obsoleto (compatibilidad)
public class StoredLicense
{
    // ... otros campos

    [Obsolete("SSL no necesita guardarse con la nueva API v1.1")]
    public string SslNumber { get; set; }
}
```

---

### **Fase 4: Validaciones Sin SSL**

**Objetivo:** No enviar SSL en validaciones posteriores

**Modificar métodos de validación:**

```csharp
// ForceOnlineValidation() - LÍNEA 232
var serverResult = apiClient.ValidateLicenseHardware(
    storedLicense.LicenseKey,
    fp,
    storedLicense.ActivationId
    // ELIMINAR: storedLicense.SslNumber
);

// PerformOnlineValidationHardware() - LÍNEA 461
return apiClient.ValidateLicenseHardware(
    license.LicenseKey,
    fp,
    license.ActivationId
    // ELIMINAR: license.SslNumber
);
```

---

### **Fase 5: Mensajes UX Mejorados**

**Objetivo:** Informar claramente al usuario según el estado

```csharp
// En ActivateLicense()
if (sslInfo.IsFirstActivation)
{
    error = "PRIMERA ACTIVACIÓN: Esta licencia requiere su número SSL. " +
            "Por favor ingrese el SSL que aparece en su factura o documento.";
    return false;
}

// Después de activación exitosa
if (response.Ssl?.Used == true && response.Ssl?.FirstActivation != null)
{
    // Informar al usuario
    Console.WriteLine("✅ Activación exitosa. Ya no necesitará el número SSL en el futuro.");
}
```

---

## 🔧 **CAMBIOS ESPECÍFICOS POR ARCHIVO**

### **1. WincajaLicenseManager/Models/LicenseModels.cs**

```csharp
// CAMBIO 1: StoredLicense
// LÍNEA 135 - ELIMINAR o marcar obsoleto
public class StoredLicense
{
    // ... otros campos
    // OPCIÓN A: Eliminar línea 135
    // OPCIÓN B: Marcar como obsoleto
    [Obsolete("SSL no necesita guardarse con API v1.1")]
    public string SslNumber { get; set; }
}

// CAMBIO 2: Agregar nueva clase helper (OPCIONAL)
public class SslRequirementInfo
{
    public bool IsRequired { get; set; }
    public bool IsFirstActivation { get; set; }
    public bool IsMigrated { get; set; }
    public string Message { get; set; }
    public string LegacySslNumber { get; set; }
}
```

---

### **2. WincajaLicenseManager/Core/LicenseValidator.cs**

```csharp
// CAMBIO 1: Agregar método CheckSslRequirement
// INSERTAR DESPUÉS DE LÍNEA 530
public SslRequirementInfo CheckSslRequirement(string licenseKey, out string error)
{
    error = null;
    var info = new SslRequirementInfo();

    try
    {
        using (var apiClient = new ApiClient())
        {
            // Validar sin hardware check para obtener info SSL
            var response = apiClient.ValidateLicense(licenseKey);

            if (response?.Ssl != null)
            {
                info.IsRequired = response.Ssl.Required;
                info.IsFirstActivation = !response.Ssl.Used; // ← CLAVE
                info.IsMigrated = response.Ssl.MigratedFromLegacy;
                info.LegacySslNumber = response.Ssl.LegacySslNumber;

                if (info.IsFirstActivation && info.IsRequired)
                {
                    info.Message = "Esta licencia requiere SSL para primera activación";
                }
                else if (info.IsRequired && !info.IsFirstActivation)
                {
                    info.Message = "Esta licencia ya fue activada, SSL no es necesario";
                }
                else
                {
                    info.Message = "Esta licencia no requiere SSL";
                }
            }

            return info;
        }
    }
    catch (Exception ex)
    {
        error = $"Error al verificar requisitos SSL: {ex.Message}";
        return info;
    }
}

// CAMBIO 2: Modificar ActivateLicense
// LÍNEA 532-593 - ACTUALIZAR LÓGICA
public bool ActivateLicense(string licenseKey, out string error, string sslNumber = null)
{
    error = null;

    try
    {
        // NUEVO: Verificar si necesita SSL
        var sslInfo = CheckSslRequirement(licenseKey, out var checkError);

        if (!string.IsNullOrEmpty(checkError))
        {
            error = checkError;
            return false;
        }

        // NUEVO: Si es primera activación de licencia migrada y NO se proporcionó SSL
        if (sslInfo.IsFirstActivation && sslInfo.IsRequired && string.IsNullOrEmpty(sslNumber))
        {
            error = "SSL_REQUIRED_FOR_FIRST_ACTIVATION: Esta licencia requiere su número SSL para la primera activación. " +
                    "Por favor proporcione el SSL que aparece en su documento de licencia.";
            return false;
        }

        // NUEVO: Si NO es primera activación y se proporcionó SSL, advertir que no es necesario
        if (!sslInfo.IsFirstActivation && !string.IsNullOrEmpty(sslNumber))
        {
            Console.WriteLine("[INFO] SSL proporcionado pero no es necesario (licencia ya activada previamente)");
            // Continuar sin error, el servidor lo manejará
        }

        // Get hardware info
        var hardwareInfo = _fingerprinter.GetSimplifiedHardwareInfo();
        var fingerprint = _fingerprinter.GetHardwareFingerprint();

        // Call activation API
        using (var apiClient = new ApiClient())
        {
            var response = apiClient.ActivateLicense(licenseKey, hardwareInfo, sslNumber);

            if (!response.Success)
            {
                // Manejar errores específicos de SSL
                if (response.Error?.Contains("SSL_REQUIRED") == true)
                {
                    error = "SSL_REQUIRED: Esta licencia requiere un número SSL para activación. " +
                            "Por favor proporcione el número SSL.";
                }
                else if (response.Error?.Contains("SSL_MISMATCH") == true)
                {
                    error = "SSL_MISMATCH: El número SSL proporcionado no coincide con el registrado. " +
                            "Verifique el SSL en su documento de licencia.";
                }
                else if (response.Error?.Contains("ACTIVATION_LIMIT_EXCEEDED") == true ||
                         response.Error?.Contains("Activation limit reached") == true)
                {
                    error = "ACTIVATION_LIMIT_EXCEEDED: Esta licencia ya alcanzó el límite de activaciones permitidas.";
                }
                else
                {
                    error = response.Error ?? "Activation failed";
                }
                return false;
            }

            // NUEVO: Informar al usuario si fue primera activación exitosa
            if (response.Ssl?.Used == true && response.Ssl?.FirstActivation != null)
            {
                Console.WriteLine("✅ Primera activación exitosa. Ya no necesitará el número SSL para futuras activaciones.");
            }

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
                // ELIMINADO: SslNumber = sslNumber
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

// CAMBIO 3: Actualizar ForceOnlineValidation
// LÍNEA 232 - ELIMINAR envío de SSL
var serverResult = apiClient.ValidateLicenseHardware(
    storedLicense.LicenseKey,
    fp,
    storedLicense.ActivationId
    // ELIMINAR: , storedLicense.SslNumber
);

// CAMBIO 4: Actualizar PerformOnlineValidationHardware
// LÍNEA 461 - ELIMINAR envío de SSL
return apiClient.ValidateLicenseHardware(
    license.LicenseKey,
    fp,
    license.ActivationId
    // ELIMINAR: , license.SslNumber
);
```

---

### **3. WincajaLicenseManager/Core/ApiClient.cs**

```csharp
// CAMBIO 1: Agregar método ValidateLicense (sin hardware check)
// INSERTAR DESPUÉS DE LÍNEA 360 (antes de ValidateLicenseHardware)

public async Task<ValidationResponse> ValidateLicenseAsync(string licenseKey)
{
    try
    {
        var request = new
        {
            licenseKey = licenseKey,
            includeHardwareCheck = false
        };

        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };
        var json = JsonConvert.SerializeObject(request, jsonSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/validate", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettings);

            if (result != null)
            {
                result.HasLicense = CalculateHasLicense(result);
            }

            return result ?? new ValidationResponse { Success = false, Error = "Invalid response from server" };
        }
        else
        {
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseContent, jsonSettings);
                if (errorResponse != null)
                {
                    errorResponse.StatusCode = (int)response.StatusCode;
                    errorResponse.HasLicense = CalculateHasLicense(errorResponse);
                    return errorResponse;
                }
                return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
            }
            catch
            {
                return new ValidationResponse { Success = false, Error = $"Server error: {response.StatusCode}", StatusCode = (int)response.StatusCode };
            }
        }
    }
    catch (TaskCanceledException)
    {
        return new ValidationResponse { Success = false, Error = "Request timed out" };
    }
    catch (HttpRequestException ex)
    {
        return new ValidationResponse { Success = false, Error = $"Network error: {ex.Message}" };
    }
    catch (Exception ex)
    {
        return new ValidationResponse { Success = false, Error = $"Unexpected error: {ex.Message}" };
    }
}

public ValidationResponse ValidateLicense(string licenseKey)
{
    try
    {
        var task = Task.Run(async () => await ValidateLicenseAsync(licenseKey));
        return task.Result;
    }
    catch (AggregateException ex)
    {
        var innerEx = ex.InnerException;
        return new ValidationResponse { Success = false, Error = innerEx?.Message ?? "Validation failed" };
    }
}

// CAMBIO 2: Actualizar ValidateLicenseHardware para no requerir SSL
// LÍNEA 361 - Hacer sslNumber realmente opcional (sin enviar si es null)
// YA ESTÁ IMPLEMENTADO CORRECTAMENTE con Dictionary<string, object>
```

---

### **4. WincajaLicenseManager/IWincajaLicenseManager.cs**

```csharp
// CAMBIO 1: Agregar método para verificar requisitos SSL
// INSERTAR nuevo método
[DispId(9)]
string CheckSslRequirement(string licenseKey);
// Retorna JSON con SslRequirementInfo

// CAMBIO 2: Actualizar documentación de ActivateLicense
[DispId(1)]
bool ActivateLicense(
    string licenseKey,
    [MarshalAs(UnmanagedType.BStr)] out string error,
    [Optional, DefaultParameterValue(null)] string sslNumber
);
// Nota: sslNumber solo necesario en primera activación de licencias migradas
```

---

### **5. WincajaLicenseManager/WincajaLicenseManagerImpl.cs**

```csharp
// CAMBIO 1: Implementar CheckSslRequirement
public string CheckSslRequirement(string licenseKey)
{
    try
    {
        var info = _validator.CheckSslRequirement(licenseKey, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            return JsonConvert.SerializeObject(new
            {
                success = false,
                error = error
            });
        }

        return JsonConvert.SerializeObject(new
        {
            success = true,
            isRequired = info.IsRequired,
            isFirstActivation = info.IsFirstActivation,
            isMigrated = info.IsMigrated,
            message = info.Message,
            legacySslNumber = info.LegacySslNumber // Solo para debug, NO mostrar al usuario
        });
    }
    catch (Exception ex)
    {
        return JsonConvert.SerializeObject(new
        {
            success = false,
            error = ex.Message
        });
    }
}
```

---

## 📝 **RESUMEN DE CAMBIOS**

### **Archivos a Modificar:**

1. ✅ `LicenseModels.cs`

   - Eliminar/obsoleto `SslNumber` de `StoredLicense`
   - Agregar `SslRequirementInfo` (opcional)

2. ✅ `LicenseValidator.cs`

   - Agregar `CheckSslRequirement()`
   - Modificar `ActivateLicense()` con lógica inteligente
   - Actualizar `ForceOnlineValidation()` - eliminar SSL
   - Actualizar `PerformOnlineValidationHardware()` - eliminar SSL

3. ✅ `ApiClient.cs`

   - Agregar `ValidateLicense()` sin hardware check
   - Mantener `ValidateLicenseHardware()` actual (ya está bien)

4. ✅ `IWincajaLicenseManager.cs`

   - Agregar `CheckSslRequirement()`

5. ✅ `WincajaLicenseManagerImpl.cs`
   - Implementar `CheckSslRequirement()`

### **Archivos a Crear:**

6. ✅ `test-nueva-logica-ssl.ps1` - Script de prueba
7. ✅ `ADAPTACION-API-v1.1-SUMMARY.md` - Documentación

---

## 🧪 **CASOS DE PRUEBA**

### **Caso 1: Primera Activación - Licencia Migrada**

```
1. CheckSslRequirement("PI7R8-KYMC-...")
   → isFirstActivation = true, isRequired = true
2. ActivateLicense("PI7R8-KYMC-...", null)
   → Error: SSL_REQUIRED_FOR_FIRST_ACTIVATION
3. ActivateLicense("PI7R8-KYMC-...", "SL11A13197")
   → Success = true
```

### **Caso 2: Reactivación - Licencia Migrada**

```
1. CheckSslRequirement("PI7R8-KYMC-...")
   → isFirstActivation = false, isRequired = true
2. ActivateLicense("PI7R8-KYMC-...", null)
   → Success = true (SIN SSL)
```

### **Caso 3: Licencia Nueva**

```
1. CheckSslRequirement("ABCD-1234-...")
   → isFirstActivation = false, isRequired = false
2. ActivateLicense("ABCD-1234-...", null)
   → Success = true
```

---

## ✅ **VERIFICACIÓN FINAL**

- [ ] SSL NO se guarda localmente
- [ ] SSL NO se envía en validaciones
- [ ] SSL solo se solicita en primera activación
- [ ] Mensajes claros para el usuario
- [ ] Compatible con licencias nuevas y migradas
- [ ] Tests pasan correctamente

---

## 📚 **DOCUMENTACIÓN USUARIO FINAL**

### **Para el Equipo .NET:**

**Primera vez que activan una licencia migrada:**

```csharp
// 1. Verificar si necesita SSL
var info = manager.CheckSslRequirement(licenseKey);
if (info.IsFirstActivation && info.IsRequired)
{
    // 2. Solicitar SSL al usuario
    var ssl = ShowSslInputDialog();

    // 3. Activar con SSL
    var result = manager.ActivateLicense(licenseKey, out error, ssl);
}
else
{
    // 4. Activar sin SSL
    var result = manager.ActivateLicense(licenseKey, out error);
}
```

**Reactivaciones (cambio de máquina):**

```csharp
// Ya no necesita SSL
var result = manager.ActivateLicense(licenseKey, out error);
// ✅ Funciona automáticamente
```

---

**Fecha del Plan:** 14 de octubre de 2025
**Versión API Objetivo:** v1.1
**Estado:** Listo para implementar
