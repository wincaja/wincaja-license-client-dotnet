# Script de Prueba - Campo HasLicense
# Este script verifica que el nuevo campo HasLicense funciona correctamente

Write-Host "=== PRUEBA DEL CAMPO HasLicense ===" -ForegroundColor Green
Write-Host ""

Write-Host "Nuevo campo agregado:" -ForegroundColor Yellow
Write-Host "- HasLicense: Indica si la PC tiene licencia (SÍ/NO)"
Write-Host "- Se calcula automáticamente basado en los datos del servidor"
Write-Host "- Elimina confusión entre 'válida para usar' vs 'ya activada'"
Write-Host ""

Write-Host "Probando ForceOnlineValidation..." -ForegroundColor Cyan
try {
    $result = $licenseManager.ValidateLicenseForceOnline()
    Write-Host "Resultado ForceOnlineValidation:" -ForegroundColor Green
    Write-Host $result
    Write-Host ""
    
    # Verificar el nuevo campo HasLicense
    $resultObj = $result | ConvertFrom-Json
    if ($resultObj.hasLicense -eq $true) {
        Write-Host "✅ HasLicense: TRUE - La PC SÍ tiene licencia" -ForegroundColor Green
    } else {
        Write-Host "❌ HasLicense: FALSE - La PC NO tiene licencia" -ForegroundColor Red
    }
    
    # Mostrar comparación de campos
    Write-Host ""
    Write-Host "Comparación de campos:" -ForegroundColor Yellow
    Write-Host "- Valid: $($resultObj.valid) (¿Se puede usar AHORA?)"
    Write-Host "- HasLicense: $($resultObj.hasLicense) (¿Tiene licencia?)"
    Write-Host "- Success: $($resultObj.success) (Campo técnico)"
    Write-Host ""
    
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
    
    $resultObj2 = $result2 | ConvertFrom-Json
    if ($resultObj2.hasLicense -eq $true) {
        Write-Host "✅ HasLicense: TRUE - La PC SÍ tiene licencia" -ForegroundColor Green
    } else {
        Write-Host "❌ HasLicense: FALSE - La PC NO tiene licencia" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error en ValidateLicense: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== VERIFICACIÓN DE DEBUG OUTPUT ===" -ForegroundColor Green
Write-Host "Revisa la consola para ver los mensajes de debug:"
Write-Host "- [DEBUG] ValidateLicenseHardwareAsync - HasLicense=True/False"
Write-Host "- [DEBUG] ValidateLicenseAsync - HasLicense=True/False"
Write-Host ""

Write-Host "=== CASOS DE USO ===" -ForegroundColor Green
Write-Host "1. PC sin licencia:"
Write-Host "   - Valid: false"
Write-Host "   - HasLicense: false"
Write-Host "   - Acción: Mostrar mensaje 'No tiene licencia'"
Write-Host ""
Write-Host "2. PC con licencia (ya activada):"
Write-Host "   - Valid: false"
Write-Host "   - HasLicense: true"
Write-Host "   - Acción: Mostrar mensaje 'Tiene licencia activada'"
Write-Host ""
Write-Host "3. PC con licencia (disponible):"
Write-Host "   - Valid: true"
Write-Host "   - HasLicense: true"
Write-Host "   - Acción: Permitir uso normal"
Write-Host ""

Write-Host "=== RESUMEN ===" -ForegroundColor Green
Write-Host "El campo HasLicense elimina la confusión y da una respuesta clara:"
Write-Host "- true = La PC tiene licencia (activada o disponible)"
Write-Host "- false = La PC NO tiene licencia"
Write-Host ""
Write-Host "¡Ahora el equipo de .NET puede usar solo HasLicense para saber si tiene licencia!" -ForegroundColor Green
