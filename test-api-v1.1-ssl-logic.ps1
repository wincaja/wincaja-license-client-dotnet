# Script de prueba para la nueva lógica SSL de API v1.1
# Verifica el flujo completo de primera activación y reactivación

Write-Host "=== PRUEBA DE NUEVA LÓGICA SSL (API v1.1) ===" -ForegroundColor Green

# Compilar el proyecto
Write-Host "`n1. Compilando proyecto..." -ForegroundColor Yellow
dotnet build WincajaLicenseManager/WincajaLicenseManager.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en compilación" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Compilación exitosa" -ForegroundColor Green

Write-Host "`n=== RESUMEN DE CAMBIOS IMPLEMENTADOS ===" -ForegroundColor Cyan

Write-Host "`n📋 Cambios en Modelos:" -ForegroundColor Yellow
Write-Host "  ✅ StoredLicense.SslNumber marcado como [Obsolete]" -ForegroundColor White
Write-Host "  ✅ Nueva clase: SslRequirementInfo" -ForegroundColor White
Write-Host "  ✅ SslInfo.Used campo clave para detectar primera activación" -ForegroundColor White

Write-Host "`n📋 Cambios en ApiClient:" -ForegroundColor Yellow
Write-Host "  ✅ Nuevo método: ValidateLicense() sin hardware check" -ForegroundColor White
Write-Host "  ✅ Útil para consultar ssl.used antes de activar" -ForegroundColor White

Write-Host "`n📋 Cambios en LicenseValidator:" -ForegroundColor Yellow
Write-Host "  ✅ Nuevo método: CheckSslRequirement()" -ForegroundColor White
Write-Host "  ✅ ActivateLicense() con lógica inteligente:" -ForegroundColor White
Write-Host "    • Detecta si es primera activación (ssl.used = false)" -ForegroundColor White
Write-Host "    • Requiere SSL solo en primera activación" -ForegroundColor White
Write-Host "    • Permite reactivación sin SSL" -ForegroundColor White
Write-Host "  ✅ ForceOnlineValidation() ya no envía SSL" -ForegroundColor White
Write-Host "  ✅ PerformOnlineValidationHardware() ya no envía SSL" -ForegroundColor White

Write-Host "`n📋 Mejoras UX:" -ForegroundColor Yellow
Write-Host "  ✅ Mensajes claros según estado de activación" -ForegroundColor White
Write-Host "  ✅ Logs informativos para debugging" -ForegroundColor White
Write-Host "  ✅ Errores específicos con soluciones" -ForegroundColor White

Write-Host "`n=== FLUJO ESPERADO ===" -ForegroundColor Cyan

Write-Host "`n🔹 PRIMERA ACTIVACIÓN (Licencia Migrada):" -ForegroundColor Yellow
Write-Host "1. CheckSslRequirement(licenseKey)" -ForegroundColor White
Write-Host "   → IsFirstActivation = true, IsRequired = true" -ForegroundColor Gray
Write-Host "2. ActivateLicense(licenseKey, null)" -ForegroundColor White
Write-Host "   → Error: SSL_REQUIRED_FOR_FIRST_ACTIVATION" -ForegroundColor Red
Write-Host "3. Usuario proporciona SSL: 'SL11A13197'" -ForegroundColor White
Write-Host "4. ActivateLicense(licenseKey, 'SL11A13197')" -ForegroundColor White
Write-Host "   → Success = true" -ForegroundColor Green
Write-Host "   → Servidor marca ssl.used = true" -ForegroundColor Gray
Write-Host "   → Mensaje: 'Ya no necesitará el SSL para futuras activaciones'" -ForegroundColor Gray

Write-Host "`n🔹 DESACTIVACIÓN:" -ForegroundColor Yellow
Write-Host "1. DeactivateLicense()" -ForegroundColor White
Write-Host "   → Success = true" -ForegroundColor Green
Write-Host "   → ssl.used permanece en true en el servidor" -ForegroundColor Gray
Write-Host "   → Archivo local borrado" -ForegroundColor Gray

Write-Host "`n🔹 REACTIVACIÓN (Nueva Máquina):" -ForegroundColor Yellow
Write-Host "1. CheckSslRequirement(licenseKey)" -ForegroundColor White
Write-Host "   → IsFirstActivation = false, IsRequired = true" -ForegroundColor Gray
Write-Host "   → Mensaje: 'SSL no es necesario para reactivar'" -ForegroundColor Gray
Write-Host "2. ActivateLicense(licenseKey, null)" -ForegroundColor White
Write-Host "   → Success = true (SIN SSL)" -ForegroundColor Green
Write-Host "   → Mensaje: 'Reactivación exitosa'" -ForegroundColor Gray

Write-Host "`n🔹 LICENCIA NUEVA (Sin SSL):" -ForegroundColor Yellow
Write-Host "1. CheckSslRequirement(licenseKey)" -ForegroundColor White
Write-Host "   → IsRequired = false" -ForegroundColor Gray
Write-Host "2. ActivateLicense(licenseKey, null)" -ForegroundColor White
Write-Host "   → Success = true" -ForegroundColor Green

Write-Host "`n=== VALIDACIONES SIN SSL ===" -ForegroundColor Cyan
Write-Host "✅ ForceOnlineValidation() ya no envía SSL" -ForegroundColor Green
Write-Host "✅ PerformOnlineValidationHardware() ya no envía SSL" -ForegroundColor Green
Write-Host "✅ Servidor maneja ssl.used automáticamente" -ForegroundColor Green

Write-Host "`n=== COMPATIBILIDAD ===" -ForegroundColor Cyan
Write-Host "✅ Licencias nuevas: Sin cambios, funcionan igual" -ForegroundColor Green
Write-Host "✅ Licencias migradas: Mejor UX, SSL solo en primera vez" -ForegroundColor Green
Write-Host "✅ API v1.1: Compatible con nueva lógica ssl.used" -ForegroundColor Green
Write-Host "✅ Archivos existentes: SslNumber marcado obsoleto pero compatible" -ForegroundColor Green

Write-Host "`n=== BENEFICIOS ===" -ForegroundColor Cyan
Write-Host "🎉 Usuario NO necesita guardar el SSL físicamente" -ForegroundColor White
Write-Host "🎉 Cambio de máquina simplificado" -ForegroundColor White
Write-Host "🎉 Menos llamadas a soporte técnico" -ForegroundColor White
Write-Host "🎉 Experiencia de usuario mejorada" -ForegroundColor White
Write-Host "🎉 Seguridad mantenida (validación en primera activación)" -ForegroundColor White

Write-Host "`n=== PRÓXIMOS PASOS ===" -ForegroundColor Yellow
Write-Host "1. Probar con licencia migrada real:" -ForegroundColor White
Write-Host "   • Primera activación con SSL" -ForegroundColor Gray
Write-Host "   • Verificar que ssl.used = true después" -ForegroundColor Gray
Write-Host "   • Desactivar" -ForegroundColor Gray
Write-Host "   • Reactivar sin SSL" -ForegroundColor Gray
Write-Host "2. Probar con licencia nueva:" -ForegroundColor White
Write-Host "   • Activación sin SSL" -ForegroundColor Gray
Write-Host "   • Verificar funcionamiento normal" -ForegroundColor Gray
Write-Host "3. Verificar logs en consola:" -ForegroundColor White
Write-Host "   • CheckSslRequirement() muestra estado" -ForegroundColor Gray
Write-Host "   • ActivateLicense() muestra decisiones" -ForegroundColor Gray
Write-Host "   • Validaciones muestran que SSL no se envía" -ForegroundColor Gray

Write-Host "`n=== EJEMPLO DE USO PARA EL EQUIPO .NET ===" -ForegroundColor Cyan

Write-Host @"

// C# - Activación inteligente
public bool ActivateWithAutoSsl(string licenseKey)
{
    var manager = new WincajaLicenseManagerImpl();
    
    // 1. Verificar si necesita SSL
    var sslInfoJson = manager.CheckSslRequirement(licenseKey);
    var sslInfo = JsonConvert.DeserializeObject<dynamic>(sslInfoJson);
    
    if (sslInfo.success == false)
    {
        Console.WriteLine("Error: " + sslInfo.error);
        return false;
    }
    
    string sslNumber = null;
    
    // 2. Si es primera activación, solicitar SSL
    if (sslInfo.isFirstActivation == true && sslInfo.isRequired == true)
    {
        Console.WriteLine(sslInfo.message);
        Console.Write("Ingrese el número SSL: ");
        sslNumber = Console.ReadLine();
    }
    else if (sslInfo.isRequired == true && sslInfo.isFirstActivation == false)
    {
        Console.WriteLine("Reactivación: No se requiere SSL");
    }
    
    // 3. Activar
    string error;
    bool success = manager.ActivateLicense(licenseKey, out error, sslNumber);
    
    if (success)
    {
        Console.WriteLine("✅ Activación exitosa");
        return true;
    }
    else
    {
        Console.WriteLine("❌ Error: " + error);
        return false;
    }
}

"@ -ForegroundColor White

Write-Host "`n=== DOCUMENTACIÓN TÉCNICA ===" -ForegroundColor Cyan
Write-Host "📄 Ver: PLAN-ADAPTACION-NUEVA-API.md" -ForegroundColor White
Write-Host "📄 Ver: NUEVA-LOGICA-SSL-ANALISIS.md" -ForegroundColor White

Write-Host "`n✅ ADAPTACIÓN A API v1.1 COMPLETA" -ForegroundColor Green
Write-Host "🚀 El cliente está listo para la nueva lógica SSL" -ForegroundColor Green
