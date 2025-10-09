# Script de Prueba para Integración SSL - WinCaja License Manager
# Este script prueba los nuevos métodos SSL implementados

Write-Host "=== PRUEBA DE INTEGRACIÓN SSL - WinCaja License Manager ===" -ForegroundColor Green
Write-Host ""

# Configuración de prueba
$testLicenseKey = "ETSB-H658-002X-XA9D-7ED6"  # Licencia migrada de ejemplo
$testSslNumber = "SSL-LEGACY-010"              # SSL de ejemplo
$testNewLicenseKey = "LICENCIA-NUEVA-123"      # Licencia nueva sin SSL

Write-Host "1. Probando CheckSslRequirement para licencia migrada..." -ForegroundColor Yellow
try {
    $sslCheckResult = $licenseManager.CheckSslRequirement($testLicenseKey)
    Write-Host "Resultado SSL Check:" -ForegroundColor Cyan
    Write-Host $sslCheckResult
    Write-Host ""
} catch {
    Write-Host "Error en CheckSslRequirement: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "2. Probando CheckSslRequirement para licencia nueva..." -ForegroundColor Yellow
try {
    $sslCheckResultNew = $licenseManager.CheckSslRequirement($testNewLicenseKey)
    Write-Host "Resultado SSL Check (Licencia Nueva):" -ForegroundColor Cyan
    Write-Host $sslCheckResultNew
    Write-Host ""
} catch {
    Write-Host "Error en CheckSslRequirement (Licencia Nueva): $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "3. Probando ValidateLicenseWithSsl con SSL correcto..." -ForegroundColor Yellow
try {
    $validationResult = $licenseManager.ValidateLicenseWithSsl($testLicenseKey, $testSslNumber)
    Write-Host "Resultado Validación con SSL:" -ForegroundColor Cyan
    Write-Host $validationResult
    Write-Host ""
} catch {
    Write-Host "Error en ValidateLicenseWithSsl: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "4. Probando ValidateLicenseWithSsl con SSL incorrecto..." -ForegroundColor Yellow
try {
    $validationResultWrong = $licenseManager.ValidateLicenseWithSsl($testLicenseKey, "SSL-WRONG-999")
    Write-Host "Resultado Validación con SSL Incorrecto:" -ForegroundColor Cyan
    Write-Host $validationResultWrong
    Write-Host ""
} catch {
    Write-Host "Error en ValidateLicenseWithSsl (SSL Incorrecto): $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "5. Probando ValidateLicenseWithSsl sin SSL (debería fallar para licencia migrada)..." -ForegroundColor Yellow
try {
    $validationResultNoSsl = $licenseManager.ValidateLicenseWithSsl($testLicenseKey, $null)
    Write-Host "Resultado Validación sin SSL:" -ForegroundColor Cyan
    Write-Host $validationResultNoSsl
    Write-Host ""
} catch {
    Write-Host "Error en ValidateLicenseWithSsl (Sin SSL): $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "6. Probando ActivateLicense con SSL..." -ForegroundColor Yellow
try {
    $activationResult = $licenseManager.ActivateLicense($testLicenseKey, $testSslNumber)
    Write-Host "Resultado Activación con SSL:" -ForegroundColor Cyan
    Write-Host $activationResult
    Write-Host ""
} catch {
    Write-Host "Error en ActivateLicense con SSL: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "7. Probando GetSslInfo..." -ForegroundColor Yellow
try {
    $sslInfoResult = $licenseManager.GetSslInfo()
    Write-Host "Resultado SSL Info:" -ForegroundColor Cyan
    Write-Host $sslInfoResult
    Write-Host ""
} catch {
    Write-Host "Error en GetSslInfo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "8. Probando activación de licencia nueva (sin SSL)..." -ForegroundColor Yellow
try {
    $activationResultNew = $licenseManager.ActivateLicense($testNewLicenseKey, $null)
    Write-Host "Resultado Activación Licencia Nueva:" -ForegroundColor Cyan
    Write-Host $activationResultNew
    Write-Host ""
} catch {
    Write-Host "Error en ActivateLicense (Licencia Nueva): $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== RESUMEN DE PRUEBAS ===" -ForegroundColor Green
Write-Host "Las pruebas han sido completadas. Revisa los resultados arriba para verificar:"
Write-Host "- Licencias migradas requieren SSL"
Write-Host "- Licencias nuevas no requieren SSL"
Write-Host "- SSL incorrecto genera error SSL_MISMATCH"
Write-Host "- SSL faltante genera error SSL_REQUIRED_NOT_PROVIDED"
Write-Host "- Activación con SSL correcto funciona"
Write-Host "- GetSslInfo devuelve información SSL actual"
Write-Host ""
Write-Host "Si todos los resultados son correctos, la integración SSL está funcionando!" -ForegroundColor Green
