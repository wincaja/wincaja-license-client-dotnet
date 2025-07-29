# Wincaja License Manager - .NET Client

A COM-visible .NET Framework library for license management in legacy VB6 applications, providing secure offline validation and hardware fingerprinting.

## Overview

This library is designed to integrate with the Wincaja VB6 application, providing:
- Hardware fingerprint generation using WMI
- Secure local license storage with AES-256 encryption
- Online and offline license validation
- COM interop for VB6 compatibility
- Grace period support for offline operations

## Features

### Hardware Fingerprinting
- Collects multiple hardware identifiers (CPU, motherboard, disks, network)
- Generates consistent SHA-256 fingerprint
- Compatible with the Node.js server implementation

### Secure Storage
- AES-256-CBC encryption with HMAC for data integrity
- Machine-specific key derivation using PBKDF2
- Stores licenses in `%APPDATA%\Wincaja\license.dat`

### License Validation
- Offline validation with grace period support
- Automatic online revalidation when needed
- Hardware fingerprint verification
- Expiration date checking

### COM Interface
- Simple JSON-based API for VB6 integration
- All methods return JSON strings for easy parsing
- Thread-safe implementation

## Building the Project

### Requirements
- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- Administrator privileges (for COM registration)

### Build Steps
1. Open `WincajaLicenseManager.sln` in Visual Studio
2. Restore NuGet packages
3. Build the solution in Release mode
4. The DLL will be automatically registered for COM interop

### Manual Registration
If automatic registration fails, use:
```cmd
cd client-dotnet\WincajaLicenseManager\bin\Release
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe WincajaLicenseManager.dll /codebase
```

## VB6 Integration

### Adding Reference
1. In VB6, go to Project → References
2. Browse and select `WincajaLicenseManager.tlb`
3. Or use late binding with `CreateObject`

### Basic Usage

```vb
Private Sub Form_Load()
    Dim licenseManager As Object
    Dim result As String
    Dim json As Object
    
    ' Create the license manager
    Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    
    ' Validate license on startup
    result = licenseManager.ValidateLicense()
    
    ' Parse JSON result (using your preferred JSON parser)
    Set json = ParseJson(result)
    
    If Not json("success") Then
        MsgBox "License validation failed: " & json("error")
        ' Handle invalid license
    End If
End Sub

' Activate a new license
Private Sub ActivateLicense(licenseKey As String)
    Dim licenseManager As Object
    Dim result As String
    
    Set licenseManager = CreateObject("WincajaLicenseManager.LicenseAPI")
    result = licenseManager.ActivateLicense(licenseKey)
    
    ' Handle activation result
End Sub
```

### JSON Response Format

All methods return JSON strings with consistent structure:

```json
{
  "success": true,
  "status": "active",
  "licenseKey": "XXXX****XXXX",
  "expiresAt": "2025-12-31",
  "daysUntilExpiration": 365,
  "error": null
}
```

## API Methods

### ActivateLicense(licenseKey As String) As String
Activates a new license key with the server.

### ValidateLicense() As String
Validates the current license, performing online check if needed.

### GetLicenseStatus() As String
Returns current license status without online validation.

### GetHardwareFingerprint() As String
Returns the hardware fingerprint for debugging/support.

### DeactivateLicense() As String
Removes the current license from the system.

## Testing

Use the included TestConsole application:
```cmd
cd client-dotnet\TestConsole\bin\Release
TestConsole.exe
```

The console app provides an interactive menu to test all functionality.

## Security Considerations

1. **Encryption Key**: The library derives encryption keys from machine-specific data. For production use, consider additional entropy sources.

2. **Public Key**: Update the RSA public key in `LicenseValidator.cs` with your actual public key for signature verification.

3. **API Endpoint**: Default endpoint is `http://localhost:5174/api/licenses`. Use HTTPS in production.

4. **COM Security**: The library runs with the permissions of the calling process. Ensure proper access controls.

## Deployment

1. Include `WincajaLicenseManager.dll` with your VB6 application
2. Register the DLL on target machines using RegAsm
3. Ensure .NET Framework 4.8 is installed
4. The library will create necessary directories on first use

## Troubleshooting

### COM Registration Failed
- Run Visual Studio as Administrator
- Use RegAsm.exe from an elevated command prompt
- Check Event Viewer for detailed errors

### Hardware Fingerprint Changes
- Some virtualization software may report inconsistent hardware
- Network adapters may change with VPN connections
- Consider implementing fingerprint tolerance in production

### License File Corruption
- Delete `%APPDATA%\Wincaja\license.dat`
- Reactivate the license

## License

This component is part of the Wincaja license management system.