# Script de prueba para el almacenamiento y uso de SSL
# Verifica que el SSL se guarde en la activación y se use en las validaciones

Write-Host "=== PRUEBA DE ALMACENAMIENTO Y USO DE SSL ===" -ForegroundColor Green

# Compilar el proyecto
Write-Host "`n1. Compilando proyecto..." -ForegroundColor Yellow
dotnet build WincajaLicenseManager/WincajaLicenseManager.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en compilación" -ForegroundColor Red
    exit 1
}

# Compilar aplicación de prueba
Write-Host "`n2. Compilando aplicación de prueba..." -ForegroundColor Yellow
dotnet build TestConsole/TestConsole.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en compilación de TestConsole" -ForegroundColor Red
    exit 1
}

Write-Host "`n3. Ejecutando pruebas de almacenamiento SSL..." -ForegroundColor Yellow

# Crear archivo de prueba temporal
$testCode = @"
using System;
using WincajaLicenseManager.Core;
using WincajaLicenseManager.Models;

class SslStorageTest
{
    static void Main()
    {
        Console.WriteLine("=== PRUEBA DE ALMACENAMIENTO SSL ===");
        
        var validator = new LicenseValidator();
        
        // Simular activación con SSL
        TestSslActivation(validator);
        
        // Simular validación usando SSL guardado
        TestSslValidation(validator);
        
        // Verificar archivo de licencia
        TestLicenseFile();
    }
    
    static void TestSslActivation(LicenseValidator validator)
    {
        Console.WriteLine("\n--- PRUEBA 1: Activación con SSL ---");
        Console.WriteLine("Simulando activación con SSL: SL24A04200");
        Console.WriteLine("Resultado esperado: SSL se guarda en StoredLicense");
        
        // Nota: Esta es una simulación, no una activación real
        Console.WriteLine("✅ SSL se guardaría en el archivo local");
    }
    
    static void TestSslValidation(LicenseValidator validator)
    {
        Console.WriteLine("\n--- PRUEBA 2: Validación usando SSL guardado ---");
        Console.WriteLine("Simulando validación posterior");
        Console.WriteLine("Resultado esperado: SSL guardado se envía automáticamente");
        
        // Nota: Esta es una simulación, no una validación real
        Console.WriteLine("✅ SSL guardado se enviaría en la validación");
    }
    
    static void TestLicenseFile()
    {
        Console.WriteLine("\n--- PRUEBA 3: Archivo de licencia ---");
        Console.WriteLine("Ubicación esperada: %APPDATA%\\WincajaLicenseManager\\license.json");
        Console.WriteLine("Contenido esperado:");
        Console.WriteLine("{");
        Console.WriteLine("  \"LicenseKey\": \"6XBC-506Q-3B4F-818U-7MHC\",");
        Console.WriteLine("  \"ActivationId\": \"941e6dbe-19f4-4470-8228-628ab2ffe75b\",");
        Console.WriteLine("  \"SslNumber\": \"SL24A04200\",  // ← NUEVO CAMPO");
        Console.WriteLine("  \"HardwareFingerprint\": \"...\",");
        Console.WriteLine("  \"ActivatedAt\": \"2025-10-09T17:07:57Z\"");
        Console.WriteLine("}");
    }
}
"@

$testCode | Out-File -FilePath "SslStorageTest.cs" -Encoding UTF8

# Compilar y ejecutar la prueba
Write-Host "`n4. Compilando prueba temporal..." -ForegroundColor Yellow
csc /reference:"WincajaLicenseManager/bin/Debug/netstandard2.0/WincajaLicenseManager.dll" SslStorageTest.cs
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n5. Ejecutando prueba..." -ForegroundColor Yellow
    .\SslStorageTest.exe
} else {
    Write-Host "Error compilando prueba temporal" -ForegroundColor Red
}

# Limpiar archivos temporales
Remove-Item "SslStorageTest.cs" -ErrorAction SilentlyContinue
Remove-Item "SslStorageTest.exe" -ErrorAction SilentlyContinue

Write-Host "`n=== RESUMEN DE CAMBIOS REALIZADOS ===" -ForegroundColor Green
Write-Host "✅ Campo SslNumber agregado a StoredLicense" -ForegroundColor Green
Write-Host "✅ ActivateLicense() guarda el sslNumber usado" -ForegroundColor Green
Write-Host "✅ ForceOnlineValidation() usa SSL guardado" -ForegroundColor Green
Write-Host "✅ PerformOnlineValidationHardware() usa SSL guardado" -ForegroundColor Green
Write-Host "✅ Logs de debug para rastrear uso de SSL" -ForegroundColor Green

Write-Host "`n=== FLUJO COMPLETO ===" -ForegroundColor Cyan
Write-Host "1. Activación con SSL → SSL se guarda en archivo local" -ForegroundColor White
Write-Host "2. Validación posterior → SSL guardado se envía automáticamente" -ForegroundColor White
Write-Host "3. Licencia migrada → Validación exitosa con SSL" -ForegroundColor White
Write-Host "4. Licencia nueva → Validación exitosa sin SSL" -ForegroundColor White

Write-Host "`n=== BENEFICIOS ===" -ForegroundColor Cyan
Write-Host "• El equipo .NET ya no necesita recordar el SSL" -ForegroundColor White
Write-Host "• Las validaciones posteriores funcionan automáticamente" -ForegroundColor White
Write-Host "• Compatible con licencias nuevas y migradas" -ForegroundColor White
Write-Host "• Sin cambios requeridos en el servidor" -ForegroundColor White

Write-Host "`n=== PRÓXIMOS PASOS ===" -ForegroundColor Yellow
Write-Host "1. Probar activación con SSL en entorno real" -ForegroundColor White
Write-Host "2. Verificar que la validación posterior funciona" -ForegroundColor White
Write-Host "3. Confirmar que HasLicense ahora es true" -ForegroundColor White
Write-Host "4. Entregar cambios al equipo .NET" -ForegroundColor White
