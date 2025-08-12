# Guía Técnica - Cliente .NET para Gestión de Licencias

## 📦 WincajaLicenseManager - Biblioteca .NET

### Descripción General
Biblioteca .NET Framework 4.8 para la gestión de licencias de software con vinculación de hardware, validación en línea y almacenamiento seguro local.

## 🏗️ Arquitectura del Cliente

### Componentes Principales

```
WincajaLicenseManager/
├── Core/                           # Núcleo del sistema
│   ├── ApiClient.cs               # Cliente HTTP para API REST
│   ├── HardwareFingerprinter.cs   # Generación de huella digital
│   ├── LicenseValidator.cs        # Motor de validación
│   └── SecureStorage.cs           # Almacenamiento encriptado
├── Models/                         
│   └── LicenseModels.cs           # DTOs y modelos
├── IWincajaLicenseManager.cs      # Interfaz pública
└── WincajaLicenseManagerImpl.cs   # Implementación principal
```

## 🔑 Componentes Detallados

### 1. ApiClient.cs
**Responsabilidad**: Comunicación HTTP con el servidor de licencias

```csharp
public class ApiClient : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    
    // Constructor con URL configurable
    public ApiClient(string baseUrl = null)
    {
        _baseUrl = baseUrl ?? "http://localhost:5173/api/licenses";
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    // Métodos principales
    public ActivationResponse ActivateLicense(string licenseKey, Dictionary<string, object> hardwareInfo)
    public ValidationResponse ValidateLicenseHardware(string licenseKey, string fingerprint, string activationId)
    public DeactivationResponse DeactivateLicense(string licenseKey, string activationId, string reason)
}
```

**Características**:
- Timeout configurable (30 segundos por defecto)
- Serialización JSON con camelCase
- Manejo robusto de errores HTTP
- Soporte para métodos síncronos y asíncronos

### 2. HardwareFingerprinter.cs
**Responsabilidad**: Recolectar información de hardware y generar fingerprint único

```csharp
public class HardwareFingerprinter
{
    public string GetHardwareFingerprint()
    {
        var info = GetDetailedHardwareInfo();
        var json = JsonConvert.SerializeObject(info, Formatting.None);
        
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    
    public Dictionary<string, object> GetSimplifiedHardwareInfo()
    {
        // Recolecta información detallada del hardware
        return new Dictionary<string, object>
        {
            ["cpu"] = GetCpuInfo(),
            ["network"] = GetNetworkInfo(),
            ["disks"] = GetDiskInfo(),
            ["baseboard"] = GetBaseboardInfo(),
            ["bios"] = GetBiosInfo(),
            ["system"] = GetSystemInfo()
        };
    }
}
```

**Información Recolectada**:
- **CPU**: Fabricante, modelo, velocidad, núcleos
- **Red**: Interfaces de red, direcciones MAC
- **Discos**: Modelos, seriales, tamaños
- **Placa Base**: Fabricante, modelo, serial
- **BIOS**: Proveedor, versión, fecha
- **Sistema**: UUID, fabricante, modelo

**Nota**: El servidor trunca el hash a 16 caracteres para mayor flexibilidad.

### 3. LicenseValidator.cs
**Responsabilidad**: Lógica central de validación de licencias

```csharp
internal class LicenseValidator
{
    private readonly SecureStorage _storage;
    private readonly HardwareFingerprinter _fingerprinter;
    private readonly int _gracePeriodDays;
    
    public LicenseStatus ValidateLicense(bool performOnlineCheck = true)
    {
        // 1. Cargar licencia almacenada
        var storedLicense = _storage.LoadLicense<StoredLicense>();
        
        // 2. Verificar expiración
        if (IsExpired(storedLicense)) 
            return InvalidStatus("expired");
        
        // 3. Verificar hardware
        if (!VerifyHardware(storedLicense))
            return InvalidStatus("hardware_mismatch");
        
        // 4. Verificar período de gracia
        if (NeedsOnlineValidation(storedLicense) && performOnlineCheck)
        {
            return PerformOnlineValidation(storedLicense);
        }
        
        return ValidStatus(storedLicense);
    }
    
    public LicenseStatus ForceOnlineValidation()
    {
        // Fuerza validación en línea ignorando período de gracia
        var storedLicense = _storage.LoadLicense<StoredLicense>();
        var serverResult = apiClient.ValidateLicenseHardware(
            storedLicense.LicenseKey,
            storedLicense.ServerHardwareFingerprint,
            storedLicense.ActivationId
        );
        
        return ProcessServerResponse(serverResult);
    }
}
```

**Flujo de Validación**:
1. Verificación local (expiración, hardware)
2. Verificación de período de gracia
3. Validación en línea si es necesaria
4. Actualización de estado local

### 4. SecureStorage.cs
**Responsabilidad**: Almacenamiento seguro y encriptado de licencias

```csharp
internal class SecureStorage
{
    private readonly string _storageDirectory;
    private readonly string _licenseFilePath;
    private readonly byte[] _encryptionKey;
    
    public SecureStorage()
    {
        // Ubicación: %APPDATA%\Wincaja\license.dat
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "Wincaja");
        _licenseFilePath = Path.Combine(_storageDirectory, "license.dat");
        
        // Clave derivada de información de la máquina
        _encryptionKey = DeriveKeyFromMachineData();
    }
    
    public void SaveLicense<T>(T license)
    {
        var json = JsonConvert.SerializeObject(license);
        var encrypted = Encrypt(json);
        File.WriteAllBytes(_licenseFilePath, encrypted);
    }
    
    public T LoadLicense<T>()
    {
        if (!File.Exists(_licenseFilePath))
            return default(T);
            
        var encrypted = File.ReadAllBytes(_licenseFilePath);
        var decrypted = Decrypt(encrypted);
        return JsonConvert.DeserializeObject<T>(decrypted);
    }
}
```

**Seguridad**:
- **Encriptación**: AES-256 con IV aleatorio
- **Derivación de Clave**: Basada en:
  - Nombre de máquina
  - Dominio de usuario
  - Versión del SO
  - Product ID de Windows
- **Protección**: El archivo no puede copiarse a otra máquina

## 📊 Modelos de Datos

### StoredLicense
```csharp
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
```

### LicenseStatus
```csharp
public class LicenseStatus
{
    public bool IsValid { get; set; }
    public string Status { get; set; }  // active, invalid, expired, etc.
    public string LicenseKey { get; set; }  // Enmascarada
    public DateTime? ExpiresAt { get; set; }
    public int DaysUntilExpiration { get; set; }
    public DateTime LastValidation { get; set; }
    public int GraceDaysRemaining { get; set; }
    public bool RequiresOnlineValidation { get; set; }
    public string Error { get; set; }
}
```

## 🔄 Flujos de Operación

### Flujo de Activación
```
1. Usuario proporciona clave de licencia
2. Cliente recolecta información de hardware
3. Envía solicitud de activación al servidor
4. Servidor valida y genera fingerprint (16 chars)
5. Cliente almacena licencia encriptada localmente
6. Retorna estado de activación
```

### Flujo de Validación
```
1. Carga licencia almacenada
2. Verifica expiración local
3. Verifica fingerprint de hardware
4. Si está en período de gracia (7 días):
   - Retorna válido sin consultar servidor
5. Si no:
   - Consulta servidor con activationId
   - Actualiza estado local
   - Retorna resultado actualizado
```

### Flujo de Desactivación
```
1. Intenta desactivación en servidor
2. Si tiene éxito:
   - Elimina archivo local
   - Retorna éxito
3. Si falla:
   - Ofrece desactivación solo local
   - Advierte que el slot sigue ocupado en servidor
```

## 🛠️ Integración en Aplicaciones

### Instalación vía NuGet (futuro)
```xml
<PackageReference Include="WincajaLicenseManager" Version="1.0.0" />
```

### Uso Básico
```csharp
using WincajaLicenseManager;

public class MiAplicacion
{
    private readonly IWincajaLicenseManager _licenseManager;
    
    public MiAplicacion()
    {
        _licenseManager = new WincajaLicenseManagerImpl();
    }
    
    public bool ActivarLicencia(string clave)
    {
        var resultado = _licenseManager.ActivateLicense(clave);
        var response = JsonConvert.DeserializeObject<dynamic>(resultado);
        return response.success;
    }
    
    public bool ValidarLicencia()
    {
        var resultado = _licenseManager.ValidateLicense();
        var response = JsonConvert.DeserializeObject<dynamic>(resultado);
        return response.success && response.status == "active";
    }
}
```

### Uso Avanzado con Manejo de Errores
```csharp
public class GestorLicencias
{
    private readonly IWincajaLicenseManager _manager;
    
    public async Task<bool> ValidarConReintentos(int maxReintentos = 3)
    {
        for (int i = 0; i < maxReintentos; i++)
        {
            try
            {
                var resultado = _manager.ValidateLicense();
                var status = JsonConvert.DeserializeObject<LicenseStatus>(resultado);
                
                if (status.RequiresOnlineValidation)
                {
                    // Forzar validación en línea
                    resultado = _manager.ValidateLicenseForceOnline();
                    status = JsonConvert.DeserializeObject<LicenseStatus>(resultado);
                }
                
                return status.IsValid;
            }
            catch (Exception ex)
            {
                if (i == maxReintentos - 1) throw;
                await Task.Delay(1000 * (i + 1)); // Backoff exponencial
            }
        }
        
        return false;
    }
}
```

## 🐛 Depuración y Diagnóstico

### Habilitar Logs Detallados
```csharp
// En ApiClient.cs
Console.WriteLine($"[DEBUG] Sending request: {json}");
Console.WriteLine($"[DEBUG] Server response ({response.StatusCode}): {responseContent}");
```

### Verificar Estado Local
```csharp
var storage = new SecureStorage();
var license = storage.LoadLicense<StoredLicense>();
Console.WriteLine($"License Key: {license?.LicenseKey}");
Console.WriteLine($"Activation ID: {license?.ActivationId}");
Console.WriteLine($"Last Validation: {license?.LastValidation}");
```

### Problemas Comunes y Soluciones

| Problema | Causa | Solución |
|----------|-------|----------|
| "No license found" | No hay licencia activada | Activar licencia |
| "Hardware mismatch" | Hardware cambió | Reactivar en nuevo hardware |
| "Network error" | Sin conexión al servidor | Verificar conectividad |
| "Invalid signature" | Falta clave RSA | Configurar claves en servidor |
| "Activation limit reached" | Límite alcanzado | Desactivar en otro dispositivo |

## 🔒 Consideraciones de Seguridad

1. **No exponer claves de licencia en logs**
   - Usar enmascaramiento: `XXXX-****-****-****-XXXX`

2. **Validar certificados SSL en producción**
   ```csharp
   ServicePointManager.ServerCertificateValidationCallback = 
       (sender, cert, chain, errors) => errors == SslPolicyErrors.None;
   ```

3. **Proteger contra manipulación de tiempo**
   - Verificar que la fecha del sistema sea razonable
   - Comparar con timestamp del servidor

4. **Ofuscar el código en producción**
   - Usar herramientas como Dotfuscator o ConfuserEx

## 📈 Métricas y Monitoreo

### Telemetría Recomendada
```csharp
public class LicenseTelemetry
{
    public void TrackActivation(string result)
    {
        // Enviar a Application Insights, Sentry, etc.
        telemetryClient.TrackEvent("LicenseActivation", new Dictionary<string, string>
        {
            ["Result"] = result,
            ["Timestamp"] = DateTime.UtcNow.ToString("O"),
            ["MachineId"] = GetMachineId()
        });
    }
}
```

## 🚀 Optimizaciones de Rendimiento

1. **Cache de Validación**
   - Cachear resultado por período de gracia
   - Evitar validaciones innecesarias

2. **Paralelización**
   - Usar `async/await` para operaciones de red
   - No bloquear UI durante validación

3. **Compresión**
   - Comprimir payload de hardware info si es muy grande

## 📝 Checklist de Implementación

- [ ] Configurar URL del servidor de producción
- [ ] Implementar manejo de errores robusto
- [ ] Agregar logs para auditoría
- [ ] Configurar período de gracia apropiado
- [ ] Implementar reintentos con backoff
- [ ] Ofuscar código para producción
- [ ] Agregar telemetría
- [ ] Documentar proceso de activación para usuarios
- [ ] Preparar FAQ para soporte
- [ ] Configurar alertas de activaciones sospechosas 