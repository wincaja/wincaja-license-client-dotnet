# Wincaja License Manager .NET Client - Complete Documentation

## Quick Reference

### API Endpoints
- **Base URL**: `https://licencias.wincaja.mx/api/licenses`
- **Activation**: `POST /activate` (No auth required)
- **Validation**: `POST /validate` (No auth required)
- **Deactivation**: `POST /deactivate` (Requires Clerk auth)

### Key Integration Points
```vb
' VB6 Quick Start
Dim licenseManager As Object
Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")

' Set production endpoint
licenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"

' Validate on startup
result = licenseManager.ValidateLicense()

' Activate new license
result = licenseManager.ActivateLicense("XXXX-XXXX-XXXX-XXXX")

' Deactivate (hybrid approach handles auth failures)
result = licenseManager.DeactivateLicense()
```

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [API Integration](#api-integration)
4. [Installation & Setup](#installation--setup)
5. [Usage Examples](#usage-examples)
6. [API Reference](#api-reference)
7. [Security](#security)
8. [Troubleshooting](#troubleshooting)

## Overview

The Wincaja License Manager .NET Client is a COM-visible .NET Framework library designed to provide license management capabilities for legacy VB6 applications. It connects to the Wincaja License Management System deployed at https://licencias.wincaja.mx to validate, activate, and manage software licenses.

### Key Features
- **Hardware Fingerprinting**: Generates unique hardware identifiers using Windows Management Instrumentation (WMI)
- **Secure Storage**: Stores licenses locally with AES-256-CBC encryption
- **Hybrid Validation**: Supports both online and offline license validation with grace periods
- **COM Interoperability**: Fully compatible with VB6 through COM interface
- **Anti-Piracy Protection**: Multiple layers of security including digital signatures and hardware binding

### System Requirements
- Windows 7 or later (32-bit or 64-bit)
- .NET Framework 4.8
- Administrator privileges for COM registration
- Internet connection for online activation/validation

## Architecture

### Component Structure

```
WincajaLicenseManager/
├── Core/                        # Core functionality
│   ├── ApiClient.cs            # HTTP API communication
│   ├── HardwareFingerprinter.cs # Hardware ID generation
│   ├── LicenseValidator.cs     # License validation logic
│   └── SecureStorage.cs        # Encrypted local storage
├── Models/                      # Data models
│   └── LicenseModels.cs        # Request/response models
├── IWincajaLicenseManager.cs   # COM interface definition
└── WincajaLicenseManagerImpl.cs # Main implementation
```

### Data Flow

1. **Activation Flow**:
   ```
   VB6 App → COM Interface → License Manager → API Client → licencias.wincaja.mx
                                            ↓
                                     Secure Storage ← License Data
   ```

2. **Validation Flow**:
   ```
   VB6 App → COM Interface → License Validator → Local Check
                                              ↓ (if needed)
                                         API Client → Server Revalidation
   ```

### Key Components

#### HardwareFingerprinter
Collects hardware information to create a unique device fingerprint:
- CPU (manufacturer, model, speed, cores)
- Network adapters (MAC addresses)
- Hard drives (model, serial number, size)
- Motherboard (manufacturer, model, serial)
- BIOS (vendor, version, release date)
- System UUID

#### SecureStorage
Manages encrypted license storage:
- Location: `%APPDATA%\Wincaja\license.dat`
- Encryption: AES-256-CBC with PBKDF2 key derivation
- Integrity: HMAC-SHA256 for tamper detection

#### ApiClient
Handles all HTTP communication with the license server:
- Endpoints: `/activate`, `/validate`, `/deactivate`
- Timeout: 30 seconds
- Error handling: Network failures, server errors, timeouts

## API Integration

### Server Endpoint
The client connects to the Wincaja License Management System deployed at:
```
https://licencias.wincaja.mx/api/licenses
```

### API Architecture
The wincaja-licencias project uses React Router v7's built-in API routes with a comprehensive middleware stack:
- **Rate Limiting**: Upstash Redis-based rate limiting
- **Caching**: LRU cache for frequently accessed data
- **Security**: Helmet headers, CORS protection
- **Logging**: Structured logging with Pino
- **Error Handling**: Consistent error responses

### Available API Operations

#### 1. License Activation
**Endpoint**: `POST /api/licenses/activate`

**Purpose**: Activates a new license key and binds it to the provided hardware fingerprint.

**Request Schema**:
```json
{
  "licenseKey": "string (required)",
  "bindingMode": "strict" | "flexible" | "tolerant" (default: "flexible"),
  "hardwareInfo": {
    "platform": "string (optional)",
    "arch": "string (optional)", 
    "hostname": "string (optional)",
    "cpuModel": "string (optional)",
    "totalMemory": "number (optional)",
    "networkInterfaces": ["string array (optional)"],
    "diskSerial": "string (optional)"
  },
  "toleranceConfig": {
    "allowedChanges": "number 0-5 (optional)"
  }
}
```

**Note**: The .NET client sends hardware info in a different format which is automatically converted by the server.

**Response (Success - 200)**:
```json
{
  "success": true,
  "activationId": "uuid-v4",
  "licenseData": {
    "licenseKey": "XXXX-****-****-XXXX",
    "product": "Product Name",
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

**Response (Error - 400/404/409)**:
```json
{
  "success": false,
  "error": "Detailed error message",
  "details": [] // Optional validation errors
}
```

#### 2. License Validation
**Endpoint**: `POST /api/licenses/validate`

**Purpose**: Validates an existing license and optionally checks hardware binding.

**Request Schema**:
```json
{
  "licenseKey": "string (required)",
  "includeHardwareCheck": "boolean (default: false)",
  "hardwareFingerprint": "string (optional)"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "valid": true,
  "status": "active" | "expired" | "suspended",
  "license": {
    "licenseKey": "XXXX-****-****-XXXX",
    "product": "Product Name",
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
    "hardwareValid": true // if includeHardwareCheck was true
  }
}
```

**Response (Invalid License - 200)**:
```json
{
  "success": true,
  "valid": false,
  "reason": "expired" | "suspended" | "invalid_signature" | "hardware_mismatch",
  "details": "Human-readable explanation"
}
```

#### 3. License Deactivation
**Endpoint**: `POST /api/licenses/deactivate`

**Purpose**: Deactivates a license activation, freeing up an activation slot.

**Authentication**: This endpoint requires authentication via Clerk session token.

**Request Schema**:
```json
{
  "licenseKey": "string (required)",
  "activationId": "uuid (required)",
  "reason": "string (optional)"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "License deactivated successfully",
  "remainingActivations": 2,
  "deactivation": {
    "id": "uuid-v4",
    "deactivatedAt": "2024-01-01T00:00:00Z",
    "reason": "User requested deactivation"
  }
}
```

**Response (Error - 401/404)**:
```json
{
  "success": false,
  "error": "Authentication required" | "License not found" | "Activation not found"
}
```

#### 4. Simple Validation (No POST body)
**Endpoint**: `POST /api/licenses/validate-simple`

**Purpose**: Lightweight validation endpoint for quick license checks.

**Request**: Query parameters instead of JSON body
```
POST /api/licenses/validate-simple?key=XXXX-XXXX-XXXX-XXXX
```

**Response**: Same as regular validation endpoint

### Authentication

#### Public Endpoints (No Auth Required)
- `POST /api/licenses/activate`
- `POST /api/licenses/validate`
- `POST /api/licenses/validate-simple`

#### Protected Endpoints (Clerk Auth Required)
- `POST /api/licenses/deactivate`
- `POST /api/licenses/generate` (Admin only)
- `POST /api/licenses/extend` (Admin only)
- `POST /api/licenses/suspend` (Admin only)
- `POST /api/licenses/unsuspend` (Admin only)
- `POST /api/licenses/revoke` (Admin only)

#### Authentication Headers
For protected endpoints, include Clerk session token:
```http
Authorization: Bearer <clerk-session-token>
Cookie: __session=<clerk-session-jwt>
```

### Rate Limiting

The API implements rate limiting using Upstash Redis:

| Endpoint | Rate Limit | Window |
|----------|------------|---------|
| `/api/licenses/activate` | 10 requests | 1 hour |
| `/api/licenses/validate` | 100 requests | 1 minute |
| `/api/licenses/deactivate` | 5 requests | 1 hour |

Rate limit headers in response:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1704067200
```

### Error Response Format

All API errors follow a consistent format:

```json
{
  "success": false,
  "error": "Human-readable error message",
  "code": "ERROR_CODE", // Optional error code
  "details": [], // Optional additional details
  "requestId": "uuid-v4" // Request tracking ID
}
```

Common error codes:
- `INVALID_LICENSE_KEY`: License key format is invalid
- `LICENSE_NOT_FOUND`: License doesn't exist
- `LICENSE_EXPIRED`: License has expired
- `MAX_ACTIVATIONS_REACHED`: Activation limit exceeded
- `HARDWARE_MISMATCH`: Hardware fingerprint doesn't match
- `RATE_LIMIT_EXCEEDED`: Too many requests
- `AUTHENTICATION_REQUIRED`: Missing or invalid auth token

### Middleware Configuration

API endpoints use composed middleware:

```typescript
// Example from api.licenses.validate.tsx
return createApiHandler(
  async (request) => {
    // Handler logic
  },
  {
    rateLimit: "validation", // Rate limit type
    cache: {
      enabled: true,
      ttl: 300, // 5 minutes
      cache: caches.licenseValidation
    },
    logging: "license", // Logger category
    errorHandling: true,
    securityHeaders: true
  }
);
```

### CORS Configuration

The API allows CORS requests with proper headers:
```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
```

### API Client Configuration

The .NET client can be configured to use different endpoints:

```csharp
// Production (default)
licenseManager.SetApiBaseUrl("https://licencias.wincaja.mx/api/licenses");

// Development
licenseManager.SetApiBaseUrl("http://localhost:5173/api/licenses");

// Custom deployment
licenseManager.SetApiBaseUrl("https://your-domain.com/api/licenses");
```

### Handling Authentication in Desktop Applications

The deactivation endpoint requires authentication, which presents challenges for desktop applications. Here's how the client handles this:

#### Current Hybrid Approach
The .NET client implements a hybrid deactivation strategy:

1. **Try Server Deactivation**: Attempts to call the authenticated endpoint
2. **Handle Auth Failure**: When receiving 401 Unauthorized, offers local-only deactivation
3. **User Choice**: Lets users decide whether to proceed with local-only deactivation

```csharp
// Example: Handling authentication failure gracefully
var result = licenseManager.DeactivateLicense();
var json = JObject.Parse(result);

if (!json["success"].Value<bool>() && json["canForceLocal"].Value<bool>())
{
    // Server deactivation failed (likely auth issue)
    if (UserConfirmsLocalDeactivation())
    {
        result = licenseManager.DeactivateLicense(true); // Force local-only
    }
}
```

#### Implementing Authentication (Future Enhancement)

For production deployments, consider these authentication strategies:

**1. API Key Authentication** (Recommended for Desktop)
```csharp
// Extend the client to support API keys
public class AuthenticatedLicenseManager : WincajaLicenseManagerImpl
{
    private string _apiKey;
    
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
        // Add to all requests
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

**2. Machine-Level Service Account**
```csharp
// Use a shared service account for all desktop installations
public async Task<string> GetMachineToken()
{
    var machineId = GetHardwareFingerprint();
    var response = await _httpClient.PostAsync("/api/auth/machine", 
        new StringContent(JsonConvert.SerializeObject(new {
            machineId = machineId,
            appId = "wincaja-desktop",
            appSecret = _appSecret // Encrypted in app
        })));
    
    var token = await response.Content.ReadAsStringAsync();
    CacheToken(token, TimeSpan.FromHours(24));
    return token;
}
```

**3. User-Specific Authentication** (OAuth2 Device Flow)
```csharp
// For user-specific operations
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

// In VB6
Private Sub AuthenticateUser()
    Dim result As String
    result = licenseManager.InitiateDeviceAuth()
    
    ' Parse device code and URL
    Dim deviceCode As String
    Dim verificationUrl As String
    
    ' Show to user
    MsgBox "Please visit: " & verificationUrl & vbCrLf & _
           "And enter code: " & deviceCode, vbInformation
    
    ' Poll for completion
    Do While Not IsAuthenticated()
        Sleep 5000 ' Wait 5 seconds
        result = licenseManager.CheckAuthStatus()
    Loop
End Sub
```

**4. Embedding Credentials** (Simple but Less Secure)
```vb
' VB6 implementation with embedded service key
Private Const SERVICE_KEY As String = "svc_key_encrypted_value"

Private Function GetDecryptedKey() As String
    ' Decrypt the service key using machine-specific data
    GetDecryptedKey = DecryptWithMachineKey(SERVICE_KEY)
End Function

Private Sub ConfigureLicenseManager()
    m_LicenseManager.SetApiKey GetDecryptedKey()
End Sub
```

#### Best Practices for Desktop Authentication

1. **Never embed user credentials** in the application
2. **Use machine-specific encryption** for any stored secrets
3. **Implement token refresh** logic for long-running applications
4. **Cache tokens appropriately** to reduce API calls
5. **Handle auth failures gracefully** with clear user messaging
6. **Consider offline scenarios** where auth servers are unreachable

#### Example: Complete Deactivation Flow with Auth

```vb
Private Function DeactivateWithAuth() As Boolean
    On Error GoTo ErrorHandler
    
    Dim result As String
    Dim needsAuth As Boolean
    
    ' First attempt without auth (backwards compatibility)
    result = m_LicenseManager.DeactivateLicense()
    
    ' Check if auth is required
    If InStr(result, """error"":""Authentication required""") > 0 Then
        needsAuth = True
    End If
    
    If needsAuth Then
        ' Try to get cached token
        Dim token As String
        token = GetCachedAuthToken()
        
        If Len(token) = 0 Then
            ' No cached token, need to authenticate
            If Not AuthenticateForDeactivation() Then
                ' Auth failed, offer local-only
                If MsgBox("Cannot connect to server. Deactivate locally only?", _
                         vbYesNo + vbQuestion) = vbYes Then
                    result = m_LicenseManager.DeactivateLicense(True)
                Else
                    DeactivateWithAuth = False
                    Exit Function
                End If
            Else
                ' Retry with auth token
                result = m_LicenseManager.DeactivateLicense()
            End If
        End If
    End If
    
    ' Check final result
    DeactivateWithAuth = InStr(result, """success"":true") > 0
    Exit Function
    
ErrorHandler:
    MsgBox "Deactivation error: " & Err.Description
    DeactivateWithAuth = False
End Function
```

### Testing API Integration

#### Using cURL
Test the API endpoints directly:

```bash
# Test activation
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

# Test validation
curl -X POST https://licencias.wincaja.mx/api/licenses/validate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "TEST-1234-5678-9ABC",
    "includeHardwareCheck": false
  }'

# Test deactivation (will fail without auth)
curl -X POST https://licencias.wincaja.mx/api/licenses/deactivate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "TEST-1234-5678-9ABC",
    "activationId": "00000000-0000-0000-0000-000000000000"
  }'
```

#### Using the Test Console
The included TestConsole application provides interactive testing:

```cmd
cd client-dotnet\TestConsole\bin\Release
TestConsole.exe
```

Menu options:
1. **Activate License**: Test new license activation
2. **Validate License**: Check current license status
3. **Get Hardware Info**: Display hardware fingerprint
4. **Deactivate License**: Test deactivation flow
5. **Test API Endpoint**: Custom API testing

#### Common Test Scenarios

**1. First-Time Activation**
```
License Key: [Enter valid key]
Expected: Success with activation details
```

**2. Duplicate Activation**
```
License Key: [Same key as above]
Expected: Error - already activated on this machine
```

**3. Max Activations**
```
License Key: [Key at activation limit]
Expected: Error - max activations reached
```

**4. Expired License**
```
License Key: [Expired key]
Expected: Validation returns expired status
```

**5. Hardware Change**
```
1. Activate on Machine A
2. Copy license.dat to Machine B
3. Validate on Machine B
Expected: Hardware mismatch (depending on binding mode)
```

## Installation & Setup

### Building from Source

1. **Prerequisites**:
   - Visual Studio 2019 or later
   - .NET Framework 4.8 SDK
   - NuGet package manager

2. **Build Steps**:
   ```cmd
   cd client-dotnet
   nuget restore WincajaLicenseManager.sln
   msbuild WincajaLicenseManager.sln /p:Configuration=Release
   ```

3. **Output Location**:
   ```
   WincajaLicenseManager\bin\Release\
   ├── WincajaLicenseManager.dll
   ├── WincajaLicenseManager.tlb
   └── Newtonsoft.Json.dll
   ```

### COM Registration

#### Automatic (Visual Studio)
Build in Release mode with Visual Studio running as Administrator.

#### Manual Registration
```cmd
cd WincajaLicenseManager\bin\Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe WincajaLicenseManager.dll /codebase /tlb
```

#### Verification
Check registration in Registry Editor:
- HKEY_CLASSES_ROOT\WincajaLicenseManager.LicenseAPI
- HKEY_CLASSES_ROOT\CLSID\{D5A7E9C3-8B4F-4D2E-A1C6-9F8B7E6D5A3C}

### VB6 Project Setup

1. **Add Reference**:
   - Open VB6 project
   - Project → References
   - Browse and select `WincajaLicenseManager.tlb`

2. **Or Use Late Binding**:
   ```vb
   Dim licenseManager As Object
   Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
   ```

## Usage Examples

### Basic VB6 Integration

```vb
Option Explicit

' Module-level variable
Private m_LicenseManager As Object

Private Sub Form_Load()
    ' Initialize license manager
    Set m_LicenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    
    ' Optional: Set custom API endpoint
    ' m_LicenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"
    
    ' Validate license on startup
    If Not ValidateLicense() Then
        MsgBox "Invalid or missing license. Application will exit.", vbCritical
        End
    End If
End Sub

Private Function ValidateLicense() As Boolean
    On Error GoTo ErrorHandler
    
    Dim result As String
    Dim jsonObj As Object
    
    ' Validate license
    result = m_LicenseManager.ValidateLicense()
    
    ' Parse JSON result (using Microsoft Script Control)
    Set jsonObj = CreateObject("MSScriptControl.ScriptControl")
    jsonObj.Language = "JScript"
    jsonObj.AddCode "function parseJSON(s) { return eval('(' + s + ')'); }"
    
    Dim parsed As Object
    Set parsed = jsonObj.Run("parseJSON", result)
    
    If parsed.success Then
        Select Case parsed.status
            Case "active"
                ValidateLicense = True
                
                ' Check expiration warning
                If parsed.daysUntilExpiration < 30 Then
                    MsgBox "License expires in " & parsed.daysUntilExpiration & " days", _
                           vbInformation, "License Expiring"
                End If
                
            Case "expired"
                MsgBox "License has expired", vbCritical
                ValidateLicense = False
                
            Case "not_activated"
                ' Show activation dialog
                ShowActivationDialog
                ValidateLicense = False
                
            Case Else
                MsgBox "Unknown license status: " & parsed.status, vbCritical
                ValidateLicense = False
        End Select
    Else
        MsgBox "License validation failed: " & parsed.error, vbCritical
        ValidateLicense = False
    End If
    
    Exit Function
    
ErrorHandler:
    MsgBox "Error validating license: " & Err.Description, vbCritical
    ValidateLicense = False
End Function

Private Sub ShowActivationDialog()
    Dim licenseKey As String
    Dim result As String
    
    ' Get license key from user
    licenseKey = InputBox("Enter your license key:", "License Activation")
    
    If Len(Trim(licenseKey)) = 0 Then Exit Sub
    
    ' Activate license
    result = m_LicenseManager.ActivateLicense(licenseKey)
    
    ' Check result (simplified parsing)
    If InStr(result, """success"":true") > 0 Then
        MsgBox "License activated successfully!", vbInformation
        ' Restart validation
        ValidateLicense
    Else
        ' Extract error message
        Dim errorStart As Long
        Dim errorEnd As Long
        Dim errorMsg As String
        
        errorStart = InStr(result, """error"":""") + 9
        errorEnd = InStr(errorStart, result, """")
        errorMsg = Mid(result, errorStart, errorEnd - errorStart)
        
        MsgBox "Activation failed: " & errorMsg, vbCritical
    End If
End Sub

' Deactivate license before uninstall
Private Sub DeactivateLicense()
    On Error Resume Next
    
    Dim result As String
    result = m_LicenseManager.DeactivateLicense()
    
    If InStr(result, """success"":true") > 0 Then
        MsgBox "License deactivated successfully", vbInformation
    End If
End Sub

' Get hardware fingerprint for support
Private Sub mnuHelpAbout_Click()
    Dim result As String
    Dim fingerprint As String
    
    result = m_LicenseManager.GetHardwareFingerprint()
    
    ' Extract fingerprint
    Dim fpStart As Long
    Dim fpEnd As Long
    
    fpStart = InStr(result, """fingerprint"":""") + 15
    fpEnd = InStr(fpStart, result, """")
    fingerprint = Mid(result, fpStart, fpEnd - fpStart)
    
    MsgBox "Hardware Fingerprint: " & fingerprint, vbInformation, "System Information"
End Sub
```

### Advanced Features

#### 1. Grace Period Handling
```vb
Private Function CheckGracePeriod(jsonResult As String) As Boolean
    ' Extract grace period info
    If InStr(jsonResult, """graceRemaining""") > 0 Then
        Dim graceDays As String
        graceDays = ExtractJsonValue(jsonResult, "graceRemaining")
        
        If IsNumeric(graceDays) Then
            MsgBox "Running in offline mode. " & graceDays & " days remaining.", _
                   vbInformation, "Offline Mode"
        End If
    End If
End Function
```

#### 2. Custom API Endpoint
```vb
Private Sub ConfigureForProduction()
    m_LicenseManager.SetApiBaseUrl "https://licencias.wincaja.mx/api/licenses"
End Sub

Private Sub ConfigureForTesting()
    m_LicenseManager.SetApiBaseUrl "http://localhost:5173/api/licenses"
End Sub
```

#### 3. Hybrid Deactivation
```vb
Private Sub SmartDeactivate()
    Dim result As String
    Dim forceLocal As Boolean
    
    ' Try server deactivation first
    result = m_LicenseManager.DeactivateLicense(False)
    
    ' Check if we can force local
    If InStr(result, """canForceLocal"":true") > 0 Then
        If MsgBox("Server deactivation failed. Deactivate locally only?", _
                  vbYesNo + vbQuestion) = vbYes Then
            ' Force local deactivation
            result = m_LicenseManager.DeactivateLicense(True)
        End If
    End If
End Sub
```

### .NET Integration Example

```csharp
using System;
using Newtonsoft.Json.Linq;

class LicenseExample
{
    static void Main()
    {
        // Create license manager
        var licenseManager = new WincajaLicenseManager.WincajaLicenseManagerImpl();
        
        // Set production endpoint
        licenseManager.SetApiBaseUrl("https://licencias.wincaja.mx/api/licenses");
        
        // Validate license
        string result = licenseManager.ValidateLicense();
        JObject json = JObject.Parse(result);
        
        if (json["success"].Value<bool>())
        {
            string status = json["status"].Value<string>();
            Console.WriteLine($"License Status: {status}");
            
            if (status == "active")
            {
                int daysRemaining = json["daysUntilExpiration"].Value<int>();
                Console.WriteLine($"Days until expiration: {daysRemaining}");
            }
        }
        else
        {
            string error = json["error"].Value<string>();
            Console.WriteLine($"Validation failed: {error}");
            
            // Try activation
            Console.Write("Enter license key: ");
            string key = Console.ReadLine();
            
            result = licenseManager.ActivateLicense(key);
            json = JObject.Parse(result);
            
            if (json["success"].Value<bool>())
            {
                Console.WriteLine("Activation successful!");
            }
            else
            {
                Console.WriteLine($"Activation failed: {json["error"]}");
            }
        }
    }
}
```

## API Reference

### IWincajaLicenseManager Interface

#### ActivateLicense
```csharp
string ActivateLicense(string licenseKey)
```
Activates a new license key with the server.

**Parameters**:
- `licenseKey`: The license key to activate (format: XXXX-XXXX-XXXX-XXXX)

**Returns**: JSON string with activation result

**Example Response**:
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
Validates the current license, performing online check if needed.

**Returns**: JSON string with validation result

**Example Response**:
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
Returns current license status without performing online validation.

**Returns**: JSON string with local license status

#### GetHardwareFingerprint
```csharp
string GetHardwareFingerprint()
```
Returns the hardware fingerprint for debugging/support purposes.

**Returns**: JSON string with hardware fingerprint

**Example Response**:
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
Deactivates and removes the current license.

**Parameters**:
- `forceLocalOnly`: (Optional) If true, skip server deactivation

**Returns**: JSON string with deactivation result

**Example Response**:
```json
{
  "success": true,
  "message": "License deactivated successfully",
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
Sets the API base URL (optional, defaults to production).

**Parameters**:
- `baseUrl`: The base URL for the license API

## Security

### Encryption Details

#### Local Storage Encryption
- **Algorithm**: AES-256-CBC
- **Key Derivation**: PBKDF2 with 10,000 iterations
- **Salt**: Machine-specific (derived from hardware)
- **IV**: Random 16 bytes per encryption
- **Integrity**: HMAC-SHA256

#### RSA Signature Verification
- **Key Size**: 4096 bits
- **Signature Algorithm**: SHA256withRSA
- **Public Key**: Embedded in binary (must match server)

### Security Best Practices

1. **Protect the Binary**:
   - Sign the DLL with a code signing certificate
   - Use obfuscation tools for production builds
   - Implement anti-debugging measures

2. **API Communication**:
   - Always use HTTPS in production
   - Implement certificate pinning if possible
   - Add request signing for additional security

3. **Key Management**:
   - Update RSA public key to match your server
   - Consider additional entropy for encryption keys
   - Rotate keys periodically

4. **Error Handling**:
   - Don't expose sensitive information in errors
   - Log security events for monitoring
   - Implement rate limiting

## Troubleshooting

### Common Issues

#### 1. COM Registration Failed
**Symptoms**: "Class not registered" error

**Solutions**:
- Run Visual Studio as Administrator
- Use 32-bit RegAsm for 32-bit applications:
  ```cmd
  C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
  ```
- Check Windows Event Log for detailed errors

#### 2. Hardware Fingerprint Changes
**Symptoms**: License validation fails after hardware change

**Causes**:
- Network adapter changes (VPN, virtual adapters)
- VM hardware changes
- Windows updates affecting WMI

**Solutions**:
- Use flexible binding mode
- Implement fingerprint tolerance
- Contact support for reactivation

#### 3. Network Errors
**Symptoms**: Timeout or connection errors

**Solutions**:
- Check firewall settings
- Verify proxy configuration
- Test with curl/Postman:
  ```bash
  curl -X POST https://licencias.wincaja.mx/api/licenses/validate \
    -H "Content-Type: application/json" \
    -d '{"licenseKey":"TEST-KEY"}'
  ```

#### 4. License File Corruption
**Symptoms**: "Failed to load license data" error

**Solutions**:
- Delete license file:
  ```cmd
  del "%APPDATA%\Wincaja\license.dat"
  ```
- Reactivate license
- Check disk permissions

### Debug Mode

Enable detailed logging:
```vb
' In VB6
Dim result As String
result = m_LicenseManager.GetLicenseStatus()
Debug.Print result  ' Full JSON response
```

### Support Information

When contacting support, provide:
1. Hardware fingerprint (from GetHardwareFingerprint)
2. License key (first and last 4 characters only)
3. Error messages and JSON responses
4. Windows version and .NET Framework version
5. Application logs if available

## Performance Considerations

### Caching
- License validation results cached for 5 minutes
- Hardware info cached for session duration
- Grace period allows offline operation

### Timeouts
- API calls: 30 seconds
- WMI queries: 5 seconds per component
- Total initialization: < 10 seconds

### Best Practices
1. Validate license asynchronously on startup
2. Cache validation results appropriately
3. Implement retry logic for transient failures
4. Use grace period for offline scenarios

## Migration Guide

### From Manual Licensing
1. Generate license keys in new system
2. Deploy client DLL with application
3. Implement validation calls
4. Migrate existing licenses gradually

### From Other License Systems
1. Export existing license data
2. Import into Wincaja system
3. Update client code to use new API
4. Test thoroughly before deployment

## Appendix

### JSON Response Schemas

#### Activation Response
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

#### Validation Response
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

#### Deactivation Response
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

### Error Codes

| Code | Description | Action |
|------|-------------|--------|
| `INVALID_KEY` | License key format invalid | Check key format |
| `KEY_NOT_FOUND` | License key doesn't exist | Verify key |
| `EXPIRED` | License has expired | Renew license |
| `MAX_ACTIVATIONS` | Activation limit reached | Deactivate other |
| `HARDWARE_MISMATCH` | Hardware changed | Contact support |
| `NETWORK_ERROR` | Can't reach server | Check connection |
| `INVALID_RESPONSE` | Server response invalid | Update client |

### Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01 | Initial release |
| 1.1.0 | 2024-02 | Added grace period support |
| 1.2.0 | 2024-03 | Implemented hybrid deactivation |
| 1.3.0 | 2024-04 | Enhanced hardware fingerprinting |

---

For additional support or questions, please contact the Wincaja development team.