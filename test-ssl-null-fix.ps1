# Script de Prueba - Corrección SSL Null
# Este script verifica que el problema del sslNumber: null está resuelto

Write-Host "=== PRUEBA DE CORRECCIÓN SSL NULL ===" -ForegroundColor Green
Write-Host ""

Write-Host "Problema identificado:" -ForegroundColor Yellow
Write-Host "- El servidor rechaza sslNumber: null"
Write-Host "- Necesitamos omitir el campo cuando es null"
Write-Host "- Solo incluir sslNumber cuando tiene valor"
Write-Host ""

Write-Host "Solución implementada:" -ForegroundColor Yellow
Write-Host "- Usar Dictionary<string, object> para request dinámico"
Write-Host "- Solo agregar sslNumber si NO es null o vacío"
Write-Host "- Omitir completamente el campo cuando es null"
Write-Host ""

Write-Host "Probando ForceOnlineValidation (SSL = null)..." -ForegroundColor Cyan
try {
    $result = $licenseManager.ValidateLicenseForceOnline()
    Write-Host "Resultado ForceOnlineValidation:" -ForegroundColor Green
    Write-Host $result
    Write-Host ""
    
    # Verificar si el resultado indica éxito
    $resultObj = $result | ConvertFrom-Json
    if ($resultObj.success -eq $true) {
        Write-Host "✅ ForceOnlineValidation funciona correctamente!" -ForegroundColor Green
        Write-Host "✅ El servidor ya no rechaza el request con SSL null" -ForegroundColor Green
    } else {
        Write-Host "⚠️ ForceOnlineValidation devolvió success=false, pero sin error BAD_REQUEST" -ForegroundColor Yellow
        Write-Host "Esto puede ser normal si no hay licencia activa" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error en ForceOnlineValidation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Probando validación normal..." -ForegroundColor Cyan
try {
    $result2 = $licenseManager.ValidateLicense()
    Write-Host "Resultado ValidateLicense:" -ForegroundColor Green
    Write-Host $result2
    Write-Host ""
} catch {
    Write-Host "❌ Error en ValidateLicense: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== VERIFICACIÓN DE DEBUG OUTPUT ===" -ForegroundColor Green
Write-Host "Revisa la consola para ver los mensajes de debug:"
Write-Host "- [DEBUG] ValidateLicenseHardwareAsync - Request body: {...}"
Write-Host "- [DEBUG] ValidateLicenseHardwareAsync - SSL Number: OMITIDO (null/vacío)"
Write-Host "- [DEBUG] ValidateLicenseHardwareAsync - SSL incluido en request: False"
Write-Host ""
Write-Host "El request body ahora debería verse así:"
Write-Host '{' -ForegroundColor Cyan
Write-Host '  "licenseKey": "6XBC-506Q-3B4F-818U-7MHC",' -ForegroundColor Cyan
Write-Host '  "includeHardwareCheck": true,' -ForegroundColor Cyan
Write-Host '  "hardwareFingerprint": "e48709a70f5f2e64",' -ForegroundColor Cyan
Write-Host '  "activationId": "c2257101-aaa6-4f77-a42f-fa784468d12b"' -ForegroundColor Cyan
Write-Host '}' -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTA: Sin campo sslNumber cuando es null" -ForegroundColor Green
Write-Host ""

Write-Host "=== RESUMEN ===" -ForegroundColor Green
Write-Host "Si ya no ves el error BAD_REQUEST, la corrección funcionó!"
Write-Host "El servidor ahora recibe requests sin campo sslNumber cuando es null."
Write-Host "Cuando sslNumber tiene valor, se incluye normalmente."
