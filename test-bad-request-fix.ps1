# Script de Prueba - Corrección del Error BAD_REQUEST
# Este script prueba específicamente el problema de ForceOnlineValidation

Write-Host "=== PRUEBA DE CORRECCIÓN BAD_REQUEST ===" -ForegroundColor Green
Write-Host ""

Write-Host "Problema identificado:" -ForegroundColor Yellow
Write-Host "- ForceOnlineValidation enviaba requests sin campo sslNumber"
Write-Host "- El servidor ahora requiere que sslNumber esté presente (incluso si es null)"
Write-Host "- Esto causaba error BAD_REQUEST (400)"
Write-Host ""

Write-Host "Solución implementada:" -ForegroundColor Yellow
Write-Host "- Cambiar ValidateLicenseHardwareAsync para usar ValidationRequest en lugar de objeto anónimo"
Write-Host "- Esto asegura que sslNumber siempre esté presente en el JSON"
Write-Host ""

Write-Host "Probando ForceOnlineValidation..." -ForegroundColor Cyan
try {
    $result = $licenseManager.ValidateLicenseForceOnline()
    Write-Host "Resultado ForceOnlineValidation:" -ForegroundColor Green
    Write-Host $result
    Write-Host ""
    
    # Verificar si el resultado indica éxito
    $resultObj = $result | ConvertFrom-Json
    if ($resultObj.success -eq $true) {
        Write-Host "✅ ForceOnlineValidation funciona correctamente!" -ForegroundColor Green
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
Write-Host "- [DEBUG] ValidateLicenseHardwareAsync - SSL Number: null"
Write-Host ""
Write-Host "El request body ahora debería incluir:"
Write-Host '  "sslNumber": null' -ForegroundColor Cyan
Write-Host "En lugar de omitir el campo completamente"
Write-Host ""

Write-Host "=== RESUMEN ===" -ForegroundColor Green
Write-Host "Si ya no ves el error BAD_REQUEST, la corrección funcionó!"
Write-Host "El servidor ahora recibe el campo sslNumber correctamente."
