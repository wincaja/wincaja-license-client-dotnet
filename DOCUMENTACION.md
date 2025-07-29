# Cliente .NET del Administrador de Licencias Wincaja - Documentación Completa

## Referencia Rápida

### Endpoints de la API
- **URL Base**: `https://licencias.wincaja.mx/api/licenses`
- **Activación**: `POST /activate` (No requiere autenticación)
- **Validación**: `POST /validate` (No requiere autenticación)
- **Desactivación**: `POST /deactivate` (Requiere autenticación Clerk)

### Puntos Clave de Integración
```vb
' Inicio Rápido VB6
Dim licenseManager As Object
Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")

' Establecer endpoint de producción
licenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"

' Validar al iniciar
result = licenseManager.ValidateLicense()

' Activar nueva licencia
result = licenseManager.ActivateLicense("XXXX-XXXX-XXXX-XXXX")

' Desactivar (enfoque híbrido maneja fallos de autenticación)
result = licenseManager.DeactivateLicense()
```

## Tabla de Contenidos
1. [Descripción General](#descripción-general)
2. [Arquitectura](#arquitectura)
3. [Integración de API](#integración-de-api)
4. [Instalación y Configuración](#instalación-y-configuración)
5. [Ejemplos de Uso](#ejemplos-de-uso)
6. [Referencia de API](#referencia-de-api)
7. [Seguridad](#seguridad)
8. [Solución de Problemas](#solución-de-problemas)

## Descripción General

El Cliente .NET del Administrador de Licencias Wincaja es una librería .NET Framework visible para COM diseñada para proporcionar capacidades de gestión de licencias a aplicaciones VB6 legacy. Se conecta al Sistema de Gestión de Licencias Wincaja desplegado en https://licencias.wincaja.mx para validar, activar y gestionar licencias de software.

### Características Principales
- **Huella Digital de Hardware**: Genera identificadores únicos de hardware usando Windows Management Instrumentation (WMI)
- **Almacenamiento Seguro**: Almacena licencias localmente con cifrado AES-256-CBC
- **Validación Híbrida**: Soporta validación de licencias online y offline con períodos de gracia
- **Interoperabilidad COM**: Completamente compatible con VB6 a través de interfaz COM
- **Protección Anti-Piratería**: Múltiples capas de seguridad incluyendo firmas digitales y vinculación por hardware

### Requisitos del Sistema
- Windows 7 o posterior (32-bit o 64-bit)
- .NET Framework 4.8
- Privilegios de administrador para registro COM
- Conexión a internet para activación/validación online

## Arquitectura

### Estructura de Componentes

```
WincajaLicenseManager/
├── Core/                        # Funcionalidad principal
│   ├── ApiClient.cs            # Comunicación HTTP con API
│   ├── HardwareFingerprinter.cs # Generación de ID de hardware
│   ├── LicenseValidator.cs     # Lógica de validación de licencias
│   └── SecureStorage.cs        # Almacenamiento local cifrado
├── Models/                      # Modelos de datos
│   └── LicenseModels.cs        # Modelos de solicitud/respuesta
├── IWincajaLicenseManager.cs   # Definición de interfaz COM
└── WincajaLicenseManagerImpl.cs # Implementación principal
```

### Flujo de Datos

1. **Flujo de Activación**:
   ```
   App VB6 → Interfaz COM → Gestor Licencias → Cliente API → licencias.wincaja.mx
                                            ↓
                                     Almacenamiento Seguro ← Datos de Licencia
   ```

2. **Flujo de Validación**:
   ```
   App VB6 → Interfaz COM → Validador Licencia → Verificación Local
                                              ↓ (si es necesario)
                                         Cliente API → Revalidación Servidor
   ```

### Componentes Clave

#### HardwareFingerprinter
Recopila información de hardware para crear una huella digital única del dispositivo:
- CPU (fabricante, modelo, velocidad, núcleos)
- Adaptadores de red (direcciones MAC)
- Discos duros (modelo, número de serie, tamaño)
- Placa madre (fabricante, modelo, serie)
- BIOS (proveedor, versión, fecha de lanzamiento)
- UUID del sistema

#### SecureStorage
Gestiona el almacenamiento cifrado de licencias:
- Ubicación: `%APPDATA%\Wincaja\license.dat`
- Cifrado: AES-256-CBC con derivación de clave PBKDF2
- Integridad: HMAC-SHA256 para detección de manipulación

#### ApiClient
Maneja toda la comunicación HTTP con el servidor de licencias:
- Endpoints: `/activate`, `/validate`, `/deactivate`
- Timeout: 30 segundos
- Manejo de errores: Fallos de red, errores de servidor, timeouts

## Integración de API

### Endpoint del Servidor
El cliente se conecta al Sistema de Gestión de Licencias Wincaja desplegado en:
```
https://licencias.wincaja.mx/api/licenses
```

### Arquitectura de la API
El proyecto wincaja-licencias usa las rutas API integradas de React Router v7 con un stack completo de middleware:
- **Limitación de Velocidad**: Limitación basada en Upstash Redis
- **Caché**: Caché LRU para datos frecuentemente accedidos
- **Seguridad**: Headers Helmet, protección CORS
- **Logging**: Logging estructurado con Pino
- **Manejo de Errores**: Respuestas de error consistentes

### Operaciones de API Disponibles

#### 1. Activación de Licencia
**Endpoint**: `POST /api/licenses/activate`

**Propósito**: Activa una nueva clave de licencia y la vincula a la huella digital de hardware proporcionada.

**Esquema de Solicitud**:
```json
{
  "licenseKey": "string (requerido)",
  "bindingMode": "strict" | "flexible" | "tolerant" (por defecto: "flexible"),
  "hardwareInfo": {
    "platform": "string (opcional)",
    "arch": "string (opcional)", 
    "hostname": "string (opcional)",
    "cpuModel": "string (opcional)",
    "totalMemory": "number (opcional)",
    "networkInterfaces": ["array de strings (opcional)"],
    "diskSerial": "string (opcional)"
  },
  "toleranceConfig": {
    "allowedChanges": "number 0-5 (opcional)"
  }
}
```

**Nota**: El cliente .NET envía información de hardware en un formato diferente que es automáticamente convertido por el servidor.

**Respuesta (Éxito - 200)**:
```json
{
  "success": true,
  "activationId": "uuid-v4",
  "licenseData": {
    "licenseKey": "XXXX-****-****-XXXX",
    "product": "Nombre del Producto",
    "version": "1.0.0",
    "expiresAt": "2025-12-31T23:59:59Z",
    "features": ["feature1", "feature2"],
    "maxActivations": 3,
    "currentActivations": 1,
    "metadata": {}
  },
  "activation": {
    "id": "uuid-v4",
    "hardwareFingerprint": "sha256-hash",
    "activatedAt": "2024-01-01T00:00:00Z"
  }
}
```

**Respuesta (Error - 400/404/409)**:
```json
{
  "success": false,
  "error": "Mensaje de error detallado",
  "details": [] // Errores de validación opcionales
}
```

#### 2. Validación de Licencia
**Endpoint**: `POST /api/licenses/validate`

**Propósito**: Valida una licencia existente y opcionalmente verifica la vinculación por hardware.

**Esquema de Solicitud**:
```json
{
  "licenseKey": "string (requerido)",
  "includeHardwareCheck": "boolean (por defecto: false)",
  "hardwareFingerprint": "string (opcional)"
}
```

**Respuesta (Éxito - 200)**:
```json
{
  "success": true,
  "valid": true,
  "status": "active" | "expired" | "suspended",
  "license": {
    "licenseKey": "XXXX-****-****-XXXX",
    "product": "Nombre del Producto",
    "version": "1.0.0",
    "expiresAt": "2025-12-31T23:59:59Z",
    "features": ["feature1", "feature2"],
    "maxActivations": 3,
    "currentActivations": 1
  },
  "validation": {
    "signatureValid": true,
    "notExpired": true,
    "statusValid": true,
    "hardwareValid": true // si includeHardwareCheck era true
  }
}
```

**Respuesta (Licencia Inválida - 200)**:
```json
{
  "success": true,
  "valid": false,
  "reason": "expired" | "suspended" | "invalid_signature" | "hardware_mismatch",
  "details": "Explicación legible para humanos"
}
```

#### 3. Desactivación de Licencia
**Endpoint**: `POST /api/licenses/deactivate`

**Propósito**: Desactiva una activación de licencia, liberando un slot de activación.

**Autenticación**: Este endpoint requiere autenticación vía token de sesión Clerk.

**Esquema de Solicitud**:
```json
{
  "licenseKey": "string (requerido)",
  "activationId": "uuid (requerido)",
  "reason": "string (opcional)"
}
```

**Respuesta (Éxito - 200)**:
```json
{
  "success": true,
  "message": "Licencia desactivada exitosamente",
  "remainingActivations": 2,
  "deactivation": {
    "id": "uuid-v4",
    "deactivatedAt": "2024-01-01T00:00:00Z",
    "reason": "Desactivación solicitada por usuario"
  }
}
```

**Respuesta (Error - 401/404)**:
```json
{
  "success": false,
  "error": "Autenticación requerida" | "Licencia no encontrada" | "Activación no encontrada"
}
```

#### 4. Validación Simple (Sin cuerpo POST)
**Endpoint**: `POST /api/licenses/validate-simple`

**Propósito**: Endpoint de validación ligero para verificaciones rápidas de licencia.

**Solicitud**: Parámetros de consulta en lugar de cuerpo JSON
```
POST /api/licenses/validate-simple?key=XXXX-XXXX-XXXX-XXXX
```

**Respuesta**: Igual que el endpoint de validación regular

### Autenticación

#### Endpoints Públicos (No Requieren Autenticación)
- `POST /api/licenses/activate`
- `POST /api/licenses/validate`
- `POST /api/licenses/validate-simple`

#### Endpoints Protegidos (Requieren Autenticación Clerk)
- `POST /api/licenses/deactivate`
- `POST /api/licenses/generate` (Solo administradores)
- `POST /api/licenses/extend` (Solo administradores)
- `POST /api/licenses/suspend` (Solo administradores)
- `POST /api/licenses/unsuspend` (Solo administradores)
- `POST /api/licenses/revoke` (Solo administradores)

#### Headers de Autenticación
Para endpoints protegidos, incluir token de sesión Clerk:
```http
Authorization: Bearer <clerk-session-token>
Cookie: __session=<clerk-session-jwt>
```

### Limitación de Velocidad

La API implementa limitación de velocidad usando Upstash Redis:

| Endpoint | Límite de Velocidad | Ventana |
|----------|---------------------|---------|
| `/api/licenses/activate` | 10 solicitudes | 1 hora |
| `/api/licenses/validate` | 100 solicitudes | 1 minuto |
| `/api/licenses/deactivate` | 5 solicitudes | 1 hora |

Headers de límite de velocidad en respuesta:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1704067200
```

### Formato de Respuesta de Error

Todos los errores de API siguen un formato consistente:

```json
{
  "success": false,
  "error": "Mensaje de error legible para humanos",
  "code": "CODIGO_ERROR", // Código de error opcional
  "details": [], // Detalles adicionales opcionales
  "requestId": "uuid-v4" // ID de seguimiento de solicitud
}
```

Códigos de error comunes:
- `INVALID_LICENSE_KEY`: El formato de la clave de licencia es inválido
- `LICENSE_NOT_FOUND`: La licencia no existe
- `LICENSE_EXPIRED`: La licencia ha expirado
- `MAX_ACTIVATIONS_REACHED`: Límite de activaciones excedido
- `HARDWARE_MISMATCH`: La huella digital de hardware no coincide
- `RATE_LIMIT_EXCEEDED`: Demasiadas solicitudes
- `AUTHENTICATION_REQUIRED`: Token de autenticación faltante o inválido

### Configuración de Middleware

Los endpoints de API usan middleware compuesto:

```typescript
// Ejemplo de api.licenses.validate.tsx
return createApiHandler(
  async (request) => {
    // Lógica del manejador
  },
  {
    rateLimit: "validation", // Tipo de límite de velocidad
    cache: {
      enabled: true,
      ttl: 300, // 5 minutos
      cache: caches.licenseValidation
    },
    logging: "license", // Categoría de logger
    errorHandling: true,
    securityHeaders: true
  }
);
```

### Configuración CORS

La API permite solicitudes CORS con headers apropiados:
```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
```

### Configuración del Cliente API

El cliente .NET puede configurarse para usar diferentes endpoints:

```csharp
// Producción (por defecto)
licenseManager.SetApiBaseUrl("https://licencias.wincaja.mx/api/licenses");

// Desarrollo
licenseManager.SetApiBaseUrl("http://localhost:5173/api/licenses");

// Despliegue personalizado
licenseManager.SetApiBaseUrl("https://tu-dominio.com/api/licenses");
```

### Manejo de Autenticación en Aplicaciones de Escritorio

El endpoint de desactivación requiere autenticación, lo que presenta desafíos para aplicaciones de escritorio. Así es como el cliente maneja esto:

#### Enfoque Híbrido Actual
El cliente .NET implementa una estrategia de desactivación híbrida:

1. **Intentar Desactivación en Servidor**: Intenta llamar al endpoint autenticado
2. **Manejar Fallo de Autenticación**: Al recibir 401 No Autorizado, ofrece desactivación solo-local
3. **Elección del Usuario**: Permite a los usuarios decidir si proceder con desactivación solo-local

```csharp
// Ejemplo: Manejar fallo de autenticación elegantemente
var result = licenseManager.DeactivateLicense();
var json = JObject.Parse(result);

if (!json["success"].Value<bool>() && json["canForceLocal"].Value<bool>())
{
    // Desactivación en servidor falló (probablemente problema de auth)
    if (UserConfirmsLocalDeactivation())
    {
        result = licenseManager.DeactivateLicense(true); // Forzar solo-local
    }
}
```

#### Implementar Autenticación (Mejora Futura)

Para despliegues de producción, considera estas estrategias de autenticación:

**1. Autenticación por Clave API** (Recomendado para Escritorio)
```csharp
// Extender el cliente para soportar claves API
public class AuthenticatedLicenseManager : WincajaLicenseManagerImpl
{
    private string _apiKey;
    
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
        // Agregar a todas las solicitudes
    }
    
    protected override HttpRequestMessage PrepareRequest(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Add("X-API-Key", _apiKey);
        }
        return request;
    }
}
```

**2. Cuenta de Servicio a Nivel de Máquina**
```csharp
// Usar una cuenta de servicio compartida para todas las instalaciones de escritorio
public async Task<string> GetMachineToken()
{
    var machineId = GetHardwareFingerprint();
    var response = await _httpClient.PostAsync("/api/auth/machine", 
        new StringContent(JsonConvert.SerializeObject(new {
            machineId = machineId,
            appId = "wincaja-desktop",
            appSecret = _appSecret // Cifrado en app
        })));
    
    var token = await response.Content.ReadAsStringAsync();
    CacheToken(token, TimeSpan.FromHours(24));
    return token;
}
```

**3. Autenticación Específica de Usuario** (Flujo OAuth2 Device)
```csharp
// Para operaciones específicas de usuario
public async Task<DeviceCodeResponse> InitiateDeviceAuth()
{
    var response = await _httpClient.PostAsync("/api/auth/device/code",
        new StringContent(JsonConvert.SerializeObject(new {
            clientId = "wincaja-desktop",
            scope = "license:deactivate"
        })));
    
    return JsonConvert.DeserializeObject<DeviceCodeResponse>(
        await response.Content.ReadAsStringAsync());
}

// En VB6
Private Sub AuthenticateUser()
    Dim result As String
    result = licenseManager.InitiateDeviceAuth()
    
    ' Parsear código de dispositivo y URL
    Dim deviceCode As String
    Dim verificationUrl As String
    
    ' Mostrar al usuario
    MsgBox "Por favor visita: " & verificationUrl & vbCrLf & _
           "E ingresa el código: " & deviceCode, vbInformation
    
    ' Sondear para completar
    Do While Not IsAuthenticated()
        Sleep 5000 ' Esperar 5 segundos
        result = licenseManager.CheckAuthStatus()
    Loop
End Sub
```

**4. Incrustar Credenciales** (Simple pero Menos Seguro)
```vb
' Implementación VB6 con clave de servicio incrustada
Private Const SERVICE_KEY As String = "svc_key_encrypted_value"

Private Function GetDecryptedKey() As String
    ' Descifrar la clave de servicio usando datos específicos de máquina
    GetDecryptedKey = DecryptWithMachineKey(SERVICE_KEY)
End Function

Private Sub ConfigureLicenseManager()
    m_LicenseManager.SetApiKey GetDecryptedKey()
End Sub
```

#### Mejores Prácticas para Autenticación de Escritorio

1. **Nunca incrustes credenciales de usuario** en la aplicación
2. **Usa cifrado específico de máquina** para cualquier secreto almacenado
3. **Implementa lógica de renovación de tokens** para aplicaciones de larga duración
4. **Cachea tokens apropiadamente** para reducir llamadas API
5. **Maneja fallos de autenticación elegantemente** con mensajes claros para el usuario
6. **Considera escenarios offline** donde los servidores de auth son inaccesibles

#### Ejemplo: Flujo Completo de Desactivación con Auth

```vb
Private Function DeactivateWithAuth() As Boolean
    On Error GoTo ErrorHandler
    
    Dim result As String
    Dim needsAuth As Boolean
    
    ' Primer intento sin auth (compatibilidad hacia atrás)
    result = m_LicenseManager.DeactivateLicense()
    
    ' Verificar si se requiere auth
    If InStr(result, """error"":""Authentication required""") > 0 Then
        needsAuth = True
    End If
    
    If needsAuth Then
        ' Intentar obtener token en caché
        Dim token As String
        token = GetCachedAuthToken()
        
        If Len(token) = 0 Then
            ' No hay token en caché, necesita autenticar
            If Not AuthenticateForDeactivation() Then
                ' Auth falló, ofrecer solo-local
                If MsgBox("No se puede conectar al servidor. ¿Desactivar solo localmente?", _
                         vbYesNo + vbQuestion) = vbYes Then
                    result = m_LicenseManager.DeactivateLicense(True)
                Else
                    DeactivateWithAuth = False
                    Exit Function
                End If
            Else
                ' Reintentar con token de auth
                result = m_LicenseManager.DeactivateLicense()
            End If
        End If
    End If
    
    ' Verificar resultado final
    DeactivateWithAuth = InStr(result, """success"":true") > 0
    Exit Function
    
ErrorHandler:
    MsgBox "Error de desactivación: " & Err.Description
    DeactivateWithAuth = False
End Function
```

### Probar Integración de API

#### Usando cURL
Probar los endpoints de API directamente:

```bash
# Probar activación
curl -X POST https://licencias.wincaja.mx/api/licenses/activate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "TEST-1234-5678-9ABC",
    "bindingMode": "flexible",
    "hardwareInfo": {
      "platform": "win32",
      "hostname": "TEST-PC"
    }
  }'

# Probar validación
curl -X POST https://licencias.wincaja.mx/api/licenses/validate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "TEST-1234-5678-9ABC",
    "includeHardwareCheck": false
  }'

# Probar desactivación (fallará sin auth)
curl -X POST https://licencias.wincaja.mx/api/licenses/deactivate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "TEST-1234-5678-9ABC",
    "activationId": "00000000-0000-0000-0000-000000000000"
  }'
```

#### Usando la Consola de Pruebas
La aplicación TestConsole incluida proporciona pruebas interactivas:

```cmd
cd client-dotnet\TestConsole\bin\Release
TestConsole.exe
```

Opciones del menú:
1. **Activar Licencia**: Probar nueva activación de licencia
2. **Validar Licencia**: Verificar estado actual de licencia
3. **Obtener Info de Hardware**: Mostrar huella digital de hardware
4. **Desactivar Licencia**: Probar flujo de desactivación
5. **Probar Endpoint API**: Pruebas API personalizadas

#### Escenarios de Prueba Comunes

**1. Activación por Primera Vez**
```
Clave de Licencia: [Ingresar clave válida]
Esperado: Éxito con detalles de activación
```

**2. Activación Duplicada**
```
Clave de Licencia: [Misma clave de arriba]
Esperado: Error - ya activada en esta máquina
```

**3. Máximo de Activaciones**
```
Clave de Licencia: [Clave en límite de activación]
Esperado: Error - máximo de activaciones alcanzado
```

**4. Licencia Expirada**
```
Clave de Licencia: [Clave expirada]
Esperado: Validación devuelve estado expirado
```

**5. Cambio de Hardware**
```
1. Activar en Máquina A
2. Copiar license.dat a Máquina B
3. Validar en Máquina B
Esperado: Incompatibilidad de hardware (dependiendo del modo de vinculación)
```

## Instalación y Configuración

### Compilar desde Código Fuente

1. **Prerrequisitos**:
   - Visual Studio 2019 o posterior
   - SDK .NET Framework 4.8
   - Administrador de paquetes NuGet

2. **Pasos de Compilación**:
   ```cmd
   cd client-dotnet
   nuget restore WincajaLicenseManager.sln
   msbuild WincajaLicenseManager.sln /p:Configuration=Release
   ```

3. **Ubicación de Salida**:
   ```
   WincajaLicenseManager\bin\Release\
   ├── WincajaLicenseManager.dll
   ├── WincajaLicenseManager.tlb
   └── Newtonsoft.Json.dll
   ```

### Registro COM

#### Automático (Visual Studio)
Compilar en modo Release con Visual Studio ejecutándose como Administrador.

#### Registro Manual
```cmd
cd WincajaLicenseManager\bin\Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe WincajaLicenseManager.dll /codebase /tlb
```

#### Verificación
Verificar registro en Editor del Registro:
- HKEY_CLASSES_ROOT\WincajaLicenseManager.LicenseAPI
- HKEY_CLASSES_ROOT\CLSID\{D5A7E9C3-8B4F-4D2E-A1C6-9F8B7E6D5A3C}

### Configuración de Proyecto VB6

1. **Agregar Referencia**:
   - Abrir proyecto VB6
   - Proyecto → Referencias
   - Examinar y seleccionar `WincajaLicenseManager.tlb`

2. **O Usar Vinculación Tardía**:
   ```vb
   Dim licenseManager As Object
   Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
   ```

## Ejemplos de Uso

### Integración Básica VB6

```vb
Option Explicit

' Variable a nivel de módulo
Private m_LicenseManager As Object

Private Sub Form_Load()
    ' Inicializar administrador de licencias
    Set m_LicenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    
    ' Opcional: Establecer endpoint API personalizado
    ' m_LicenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"
    
    ' Validar licencia al iniciar
    If Not ValidateLicense() Then
        MsgBox "Licencia inválida o faltante. La aplicación se cerrará.", vbCritical
        End
    End If
End Sub

Private Function ValidateLicense() As Boolean
    On Error GoTo ErrorHandler
    
    Dim result As String
    Dim jsonObj As Object
    
    ' Validar licencia
    result = m_LicenseManager.ValidateLicense()
    
    ' Parsear resultado JSON (usando Microsoft Script Control)
    Set jsonObj = CreateObject("MSScriptControl.ScriptControl")
    jsonObj.Language = "JScript"
    jsonObj.AddCode "function parseJSON(s) { return eval('(' + s + ')'); }"
    
    Dim parsed As Object
    Set parsed = jsonObj.Run("parseJSON", result)
    
    If parsed.success Then
        Select Case parsed.status
            Case "active"
                ValidateLicense = True
                
                ' Verificar advertencia de expiración
                If parsed.daysUntilExpiration < 30 Then
                    MsgBox "La licencia expira en " & parsed.daysUntilExpiration & " días", _
                           vbInformation, "Licencia Por Expirar"
                End If
                
            Case "expired"
                MsgBox "La licencia ha expirado", vbCritical
                ValidateLicense = False
                
            Case "not_activated"
                ' Mostrar diálogo de activación
                ShowActivationDialog
                ValidateLicense = False
                
            Case Else
                MsgBox "Estado de licencia desconocido: " & parsed.status, vbCritical
                ValidateLicense = False
        End Select
    Else
        MsgBox "Falló la validación de licencia: " & parsed.error, vbCritical
        ValidateLicense = False
    End If
    
    Exit Function
    
ErrorHandler:
    MsgBox "Error validando licencia: " & Err.Description, vbCritical
    ValidateLicense = False
End Function

Private Sub ShowActivationDialog()
    Dim licenseKey As String
    Dim result As String
    
    ' Obtener clave de licencia del usuario
    licenseKey = InputBox("Ingresa tu clave de licencia:", "Activación de Licencia")
    
    If Len(Trim(licenseKey)) = 0 Then Exit Sub
    
    ' Activar licencia
    result = m_LicenseManager.ActivateLicense(licenseKey)
    
    ' Verificar resultado (parsing simplificado)
    If InStr(result, """success"":true") > 0 Then
        MsgBox "¡Licencia activada exitosamente!", vbInformation
        ' Reiniciar validación
        ValidateLicense
    Else
        ' Extraer mensaje de error
        Dim errorStart As Long
        Dim errorEnd As Long
        Dim errorMsg As String
        
        errorStart = InStr(result, """error"":""") + 9
        errorEnd = InStr(errorStart, result, """")
        errorMsg = Mid(result, errorStart, errorEnd - errorStart)
        
        MsgBox "Falló la activación: " & errorMsg, vbCritical
    End If
End Sub

' Desactivar licencia antes de desinstalar
Private Sub DeactivateLicense()
    On Error Resume Next
    
    Dim result As String
    result = m_LicenseManager.DeactivateLicense()
    
    If InStr(result, """success"":true") > 0 Then
        MsgBox "Licencia desactivada exitosamente", vbInformation
    End If
End Sub

' Obtener huella digital de hardware para soporte
Private Sub mnuHelpAbout_Click()
    Dim result As String
    Dim fingerprint As String
    
    result = m_LicenseManager.GetHardwareFingerprint()
    
    ' Extraer huella digital
    Dim fpStart As Long
    Dim fpEnd As Long
    
    fpStart = InStr(result, """fingerprint"":""") + 15
    fpEnd = InStr(fpStart, result, """")
    fingerprint = Mid(result, fpStart, fpEnd - fpStart)
    
    MsgBox "Huella Digital de Hardware: " & fingerprint, vbInformation, "Información del Sistema"
End Sub
```

### Características Avanzadas

#### 1. Manejo del Período de Gracia
```vb
Private Function CheckGracePeriod(jsonResult As String) As Boolean
    ' Extraer información del período de gracia
    If InStr(jsonResult, """graceRemaining""") > 0 Then
        Dim graceDays As String
        graceDays = ExtractJsonValue(jsonResult, "graceRemaining")
        
        If IsNumeric(graceDays) Then
            MsgBox "Ejecutándose en modo offline. " & graceDays & " días restantes.", _
                   vbInformation, "Modo Offline"
        End If
    End If
End Function
```

#### 2. Endpoint API Personalizado
```vb
Private Sub ConfigureForProduction()
    m_LicenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"
End Sub

Private Sub ConfigureForTesting()
    m_LicenseManager.SetApiBaseUrl "http://localhost:5173/api/licenses"
End Sub
```

#### 3. Desactivación Híbrida
```vb
Private Sub SmartDeactivate()
    Dim result As String
    Dim forceLocal As Boolean
    
    ' Intentar desactivación en servidor primero
    result = m_LicenseManager.DeactivateLicense(False)
    
    ' Verificar si podemos forzar local
    If InStr(result, """canForceLocal"":true") > 0 Then
        If MsgBox("Falló la desactivación en servidor. ¿Desactivar solo localmente?", _
                  vbYesNo + vbQuestion) = vbYes Then
            ' Forzar desactivación local
            result = m_LicenseManager.DeactivateLicense(True)
        End If
    End If
End Sub
```

### Ejemplo de Integración .NET

```csharp
using System;
using Newtonsoft.Json.Linq;

class LicenseExample
{
    static void Main()
    {
        // Crear administrador de licencias
        var licenseManager = new WincajaLicenseManager.WincajaLicenseManagerImpl();
        
        // Establecer endpoint de producción
        licenseManager.SetApiBaseUrl("https://licencias.wincaja.mx/api/licenses");
        
        // Validar licencia
        string result = licenseManager.ValidateLicense();
        JObject json = JObject.Parse(result);
        
        if (json["success"].Value<bool>())
        {
            string status = json["status"].Value<string>();
            Console.WriteLine($"Estado de Licencia: {status}");
            
            if (status == "active")
            {
                int daysRemaining = json["daysUntilExpiration"].Value<int>();
                Console.WriteLine($"Días hasta expiración: {daysRemaining}");
            }
        }
        else
        {
            string error = json["error"].Value<string>();
            Console.WriteLine($"Falló la validación: {error}");
            
            // Intentar activación
            Console.Write("Ingresa clave de licencia: ");
            string key = Console.ReadLine();
            
            result = licenseManager.ActivateLicense(key);
            json = JObject.Parse(result);
            
            if (json["success"].Value<bool>())
            {
                Console.WriteLine("¡Activación exitosa!");
            }
            else
            {
                Console.WriteLine($"Falló la activación: {json["error"]}");
            }
        }
    }
}
```

## Referencia de API

### Interfaz IWincajaLicenseManager

#### ActivateLicense
```csharp
string ActivateLicense(string licenseKey)
```
Activa una nueva clave de licencia con el servidor.

**Parámetros**:
- `licenseKey`: La clave de licencia a activar (formato: XXXX-XXXX-XXXX-XXXX)

**Devuelve**: String JSON con resultado de activación

**Respuesta de Ejemplo**:
```json
{
  "success": true,
  "activationId": "act_123456",
  "licenseKey": "XXXX-****-****-XXXX",
  "expiresAt": "2025-12-31",
  "features": ["feature1", "feature2"],
  "maxActivations": 3,
  "currentActivations": 1
}
```

#### ValidateLicense
```csharp
string ValidateLicense()
```
Valida la licencia actual, realizando verificación online si es necesario.

**Devuelve**: String JSON con resultado de validación

**Respuesta de Ejemplo**:
```json
{
  "success": true,
  "status": "active",
  "licenseKey": "XXXX-****-****-XXXX",
  "expiresAt": "2025-12-31",
  "daysUntilExpiration": 365,
  "requiresReactivation": false,
  "graceRemaining": 7
}
```

#### GetLicenseStatus
```csharp
string GetLicenseStatus()
```
Devuelve el estado actual de la licencia sin realizar validación online.

**Devuelve**: String JSON con estado local de licencia

#### GetHardwareFingerprint
```csharp
string GetHardwareFingerprint()
```
Devuelve la huella digital de hardware para propósitos de depuración/soporte.

**Devuelve**: String JSON con huella digital de hardware

**Respuesta de Ejemplo**:
```json
{
  "success": true,
  "fingerprint": "a1b2c3d4e5f6...",
  "components": {
    "cpu": "Intel Core i7-9700K @ 3.60GHz",
    "motherboard": "ASUS PRIME Z390-A",
    "primaryDisk": "Samsung SSD 970 EVO Plus 1TB"
  }
}
```

#### DeactivateLicense
```csharp
string DeactivateLicense()
string DeactivateLicense(bool forceLocalOnly)
```
Desactiva y remueve la licencia actual.

**Parámetros**:
- `forceLocalOnly`: (Opcional) Si es true, omite desactivación en servidor

**Devuelve**: String JSON con resultado de desactivación

**Respuesta de Ejemplo**:
```json
{
  "success": true,
  "message": "Licencia desactivada exitosamente",
  "deactivationType": "ServerAndLocal",
  "serverUpdated": true,
  "localOnly": false,
  "remainingActivations": 2
}
```

#### SetApiBaseUrl
```csharp
void SetApiBaseUrl(string baseUrl)
```
Establece la URL base de la API (opcional, por defecto a producción).

**Parámetros**:
- `baseUrl`: La URL base para la API de licencias

## Seguridad

### Detalles de Cifrado

#### Cifrado de Almacenamiento Local
- **Algoritmo**: AES-256-CBC
- **Derivación de Clave**: PBKDF2 con 10,000 iteraciones
- **Salt**: Específico de máquina (derivado de hardware)
- **IV**: 16 bytes aleatorios por cifrado
- **Integridad**: HMAC-SHA256

#### Verificación de Firma RSA
- **Tamaño de Clave**: 4096 bits
- **Algoritmo de Firma**: SHA256withRSA
- **Clave Pública**: Incrustada en binario (debe coincidir con servidor)

### Mejores Prácticas de Seguridad

1. **Proteger el Binario**:
   - Firmar la DLL con certificado de firma de código
   - Usar herramientas de ofuscación para builds de producción
   - Implementar medidas anti-depuración

2. **Comunicación API**:
   - Usar siempre HTTPS en producción
   - Implementar certificate pinning si es posible
   - Agregar firma de solicitudes para seguridad adicional

3. **Gestión de Claves**:
   - Actualizar clave pública RSA para coincidir con tu servidor
   - Considerar entropía adicional para claves de cifrado
   - Rotar claves periódicamente

4. **Manejo de Errores**:
   - No exponer información sensible en errores
   - Registrar eventos de seguridad para monitoreo
   - Implementar limitación de velocidad

## Solución de Problemas

### Problemas Comunes

#### 1. Falló el Registro COM
**Síntomas**: Error "Clase no registrada"

**Soluciones**:
- Ejecutar Visual Studio como Administrador
- Usar RegAsm de 32-bit para aplicaciones de 32-bit:
  ```cmd
  C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
  ```
- Verificar Log de Eventos de Windows para errores detallados

#### 2. Cambios en Huella Digital de Hardware
**Síntomas**: Falla validación de licencia después de cambio de hardware

**Causas**:
- Cambios en adaptador de red (VPN, adaptadores virtuales)
- Cambios de hardware de VM
- Actualizaciones de Windows afectando WMI

**Soluciones**:
- Usar modo de vinculación flexible
- Implementar tolerancia de huella digital
- Contactar soporte para reactivación

#### 3. Errores de Red
**Síntomas**: Errores de timeout o conexión

**Soluciones**:
- Verificar configuración de firewall
- Verificar configuración de proxy
- Probar con curl/Postman:
  ```bash
  curl -X POST https://licencias.wincaja.mx/api/licenses/validate \
    -H "Content-Type: application/json" \
    -d '{"licenseKey":"TEST-KEY"}'
  ```

#### 4. Corrupción de Archivo de Licencia
**Síntomas**: Error "Falló cargar datos de licencia"

**Soluciones**:
- Eliminar archivo de licencia:
  ```cmd
  del "%APPDATA%\Wincaja\license.dat"
  ```
- Reactivar licencia
- Verificar permisos de disco

### Modo de Depuración

Habilitar logging detallado:
```vb
' En VB6
Dim result As String
result = m_LicenseManager.GetLicenseStatus()
Debug.Print result  ' Respuesta JSON completa
```

### Información de Soporte

Al contactar soporte, proporcionar:
1. Huella digital de hardware (de GetHardwareFingerprint)
2. Clave de licencia (solo primeros y últimos 4 caracteres)
3. Mensajes de error y respuestas JSON
4. Versión de Windows y .NET Framework
5. Logs de aplicación si están disponibles

## Consideraciones de Rendimiento

### Caché
- Resultados de validación de licencia en caché por 5 minutos
- Información de hardware en caché por duración de sesión
- Período de gracia permite operación offline

### Timeouts
- Llamadas API: 30 segundos
- Consultas WMI: 5 segundos por componente
- Inicialización total: < 10 segundos

### Mejores Prácticas
1. Validar licencia asíncronamente al iniciar
2. Cachear resultados de validación apropiadamente
3. Implementar lógica de reintentos para fallos transitorios
4. Usar período de gracia para escenarios offline

## Guía de Migración

### Desde Licenciamiento Manual
1. Generar claves de licencia en nuevo sistema
2. Desplegar DLL cliente con aplicación
3. Implementar llamadas de validación
4. Migrar licencias existentes gradualmente

### Desde Otros Sistemas de Licencias
1. Exportar datos de licencia existentes
2. Importar al sistema Wincaja
3. Actualizar código cliente para usar nueva API
4. Probar exhaustivamente antes del despliegue

## Apéndice

### Esquemas de Respuesta JSON

#### Respuesta de Activación
```typescript
interface ActivationResponse {
  success: boolean;
  activationId?: string;
  licenseKey?: string;
  expiresAt?: string;
  features?: string[];
  maxActivations?: number;
  currentActivations?: number;
  error?: string;
}
```

#### Respuesta de Validación
```typescript
interface ValidationResponse {
  success: boolean;
  status?: 'active' | 'expired' | 'suspended' | 'not_activated';
  licenseKey?: string;
  expiresAt?: string;
  daysUntilExpiration?: number;
  requiresReactivation?: boolean;
  graceRemaining?: number;
  error?: string;
}
```

#### Respuesta de Desactivación
```typescript
interface DeactivationResponse {
  success: boolean;
  message?: string;
  deactivationType?: 'ServerAndLocal' | 'LocalOnly';
  serverUpdated?: boolean;
  localOnly?: boolean;
  warning?: string;
  canForceLocal?: boolean;
  remainingActivations?: number;
  error?: string;
}
```

### Códigos de Error

| Código | Descripción | Acción |
|--------|-------------|--------|
| `INVALID_KEY` | Formato de clave de licencia inválido | Verificar formato de clave |
| `KEY_NOT_FOUND` | Clave de licencia no existe | Verificar clave |
| `EXPIRED` | Licencia ha expirado | Renovar licencia |
| `MAX_ACTIVATIONS` | Límite de activaciones alcanzado | Desactivar otras |
| `HARDWARE_MISMATCH` | Hardware cambió | Contactar soporte |
| `NETWORK_ERROR` | No se puede alcanzar servidor | Verificar conexión |
| `INVALID_RESPONSE` | Respuesta de servidor inválida | Actualizar cliente |

### Historial de Versiones

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2024-01 | Lanzamiento inicial |
| 1.1.0 | 2024-02 | Agregado soporte de período de gracia |
| 1.2.0 | 2024-03 | Implementada desactivación híbrida |
| 1.3.0 | 2024-04 | Mejorada huella digital de hardware |

---

Para soporte adicional o preguntas, por favor contacta al equipo de desarrollo Wincaja.