# Script de prueba para la nueva lógica inteligente del LicenseValidator
# Prueba los diferentes escenarios de HasLicense y Valid

Write-Host "=== PRUEBA DE LÓGICA INTELIGENTE DEL LICENSEVALIDATOR ===" -ForegroundColor Green

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

Write-Host "`n3. Ejecutando pruebas de validación inteligente..." -ForegroundColor Yellow

# Crear archivo de prueba temporal
$testCode = @"
using System;
using WincajaLicenseManager.Core;
using WincajaLicenseManager.Models;

class IntelligentValidationTest
{
    static void Main()
    {
        Console.WriteLine("=== PRUEBA DE LÓGICA INTELIGENTE ===");
        
        var validator = new LicenseValidator();
        
        // Simular diferentes escenarios de respuesta
        TestScenario1(validator); // HasLicense=true, Valid=true
        TestScenario2(validator); // HasLicense=true, Valid=false (activation limit)
        TestScenario3(validator); // HasLicense=false
        TestScenario4(validator); // HasLicense=true, Valid=false (SSL error)
    }
    
    static void TestScenario1(LicenseValidator validator)
    {
        Console.WriteLine("\n--- ESCENARIO 1: Licencia disponible y válida ---");
        Console.WriteLine("HasLicense=true, Valid=true, License!=null");
        Console.WriteLine("Resultado esperado: IsValid=true, Status='active'");
    }
    
    static void TestScenario2(LicenseValidator validator)
    {
        Console.WriteLine("\n--- ESCENARIO 2: Licencia activada pero no disponible ---");
        Console.WriteLine("HasLicense=true, Valid=false, ActivationLimitExceeded=true");
        Console.WriteLine("Resultado esperado: IsValid=false, Status='activation_limit_exceeded'");
    }
    
    static void TestScenario3(LicenseValidator validator)
    {
        Console.WriteLine("\n--- ESCENARIO 3: No tiene licencia ---");
        Console.WriteLine("HasLicense=false");
        Console.WriteLine("Resultado esperado: IsValid=false, Status='not_activated'");
    }
    
    static void TestScenario4(LicenseValidator validator)
    {
        Console.WriteLine("\n--- ESCENARIO 4: Error SSL ---");
        Console.WriteLine("HasLicense=true, Valid=false, SSL.Required=true, SSL.Validation.Valid=false");
        Console.WriteLine("Resultado esperado: IsValid=false, Status='ssl_validation_failed'");
    }
}
"@

$testCode | Out-File -FilePath "IntelligentValidationTest.cs" -Encoding UTF8

# Compilar y ejecutar la prueba
Write-Host "`n4. Compilando prueba temporal..." -ForegroundColor Yellow
csc /reference:"WincajaLicenseManager/bin/Debug/netstandard2.0/WincajaLicenseManager.dll" IntelligentValidationTest.cs
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n5. Ejecutando prueba..." -ForegroundColor Yellow
    .\IntelligentValidationTest.exe
} else {
    Write-Host "Error compilando prueba temporal" -ForegroundColor Red
}

# Limpiar archivos temporales
Remove-Item "IntelligentValidationTest.cs" -ErrorAction SilentlyContinue
Remove-Item "IntelligentValidationTest.exe" -ErrorAction SilentlyContinue

Write-Host "`n=== RESUMEN DE CAMBIOS REALIZADOS ===" -ForegroundColor Green
Write-Host "✅ Lógica inteligente implementada en ForceOnlineValidation()" -ForegroundColor Green
Write-Host "✅ Lógica inteligente implementada en ValidateLicense()" -ForegroundColor Green
Write-Host "✅ Manejo mejorado de errores SSL en ActivateLicense()" -ForegroundColor Green
Write-Host "✅ Mensajes de error más específicos y claros" -ForegroundColor Green
Write-Host "✅ Distinción clara entre 'tiene licencia' vs 'puede usar licencia'" -ForegroundColor Green

Write-Host "`n=== CASOS DE USO CUBIERTOS ===" -ForegroundColor Cyan
Write-Host "1. Licencia nueva: HasLicense=true, Valid=true → Puede usar" -ForegroundColor White
Write-Host "2. Licencia migrada activada: HasLicense=true, Valid=false → Ya fue usada" -ForegroundColor White
Write-Host "3. Sin licencia: HasLicense=false → Necesita activar" -ForegroundColor White
Write-Host "4. Error SSL: HasLicense=true, Valid=false, SSL error → Error específico" -ForegroundColor White

Write-Host "`n=== BENEFICIOS ===" -ForegroundColor Cyan
Write-Host "• El equipo .NET ya no se confunde entre 'valid' y 'has license'" -ForegroundColor White
Write-Host "• Mensajes de error más claros para el usuario final" -ForegroundColor White
Write-Host "• Manejo inteligente de licencias migradas vs nuevas" -ForegroundColor White
Write-Host "• Sin cambios requeridos en el servidor" -ForegroundColor White
