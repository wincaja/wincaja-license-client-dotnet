# ⚠️ IMPORTANTE: Aclaración sobre "SSL" en este Proyecto

## 🔑 **Definición**

En este proyecto, **"SSL" NO es el protocolo de seguridad**.

### **SSL ≠ Secure Sockets Layer**

❌ **NO es:** Protocolo de seguridad para comunicaciones (HTTPS, TLS, etc.)

✅ **SÍ es:** **SL** = **Security Lock / Serial License Number**

**Nombres equivalentes:**

- Security Lock (nombre del sistema legacy)
- Serial License Number (número de serie)
- Número del chip físico del sistema anterior

---

## 📋 **¿Qué es el Número SL?**

El número **SL** (abreviado en el código como "SSL") es el identificador del **chip físico de seguridad** (Security Lock) del **sistema de licencias legacy** (sistema anterior) de WinCaja.

**En la base de datos histórica:** Campo `licNoChip`

### **Formato del Número SL:**

```
SL + [Año] + [Letra] + [Números]

Ejemplos:
  • SL11A13197
  • SL24A04200
  • SL25A05030
  • SL13B00542
```

**Desglose:**

- `SL` - Prefijo fijo (Serial License)
- `11`, `24`, `25` - Año de emisión (2011, 2024, 2025)
- `A`, `B` - Serie/categoría
- `13197`, `04200`, `05030` - Número secuencial

---

## 🔄 **¿Por qué se usa en el Código?**

### **Contexto: Migración del Sistema Legacy**

Cuando WinCaja migró del sistema antiguo al nuevo sistema de licencias:

1. **Sistema Legacy (Antiguo):**

   - Usaba números SL como identificador principal
   - Ejemplo: Cliente tenía licencia `SL11A13197`

2. **Sistema Nuevo:**

   - Usa License Keys en formato `XXXX-XXXX-XXXX-XXXX-XXXX`
   - Ejemplo: `PI7R8-KYMC-O4FE-RIDE-AHZY`

3. **Problema:**
   - ¿Cómo validar que una licencia nueva corresponde al cliente que tenía la licencia antigua?
   - Solución: Requerir el número SL en la primera activación

---

## 🎯 **¿Cómo se Usa?**

### **Primera Activación (Licencia Migrada):**

```csharp
// Usuario tiene:
// - Nueva License Key: "PI7R8-KYMC-O4FE-RIDE-AHZY"
// - Antiguo Número SL: "SL11A13197" (del sistema legacy)

// Para activar por primera vez, debe proporcionar AMBOS:
ActivateLicense(
    licenseKey: "PI7R8-KYMC-O4FE-RIDE-AHZY",
    sslNumber: "SL11A13197"  // ← Número del sistema antiguo
);

// El servidor valida:
// 1. ¿La License Key es válida?
// 2. ¿El número SL coincide con el registrado para esta licencia?
// 3. Si ambos son correctos → Activación exitosa
```

### **Reactivación (API v1.1):**

```csharp
// Usuario cambió de máquina y quiere reactivar

// YA NO necesita el número SL:
ActivateLicense(
    licenseKey: "PI7R8-KYMC-O4FE-RIDE-AHZY"
    // sslNumber: NO ES NECESARIO
);

// El servidor sabe que ya fue activada antes (ssl.used = true)
```

---

## 📝 **En el Código**

### **Clases que Mencionan "SSL":**

```csharp
// Todas se refieren al número SL, NO al protocolo:

public class ActivationRequest
{
    public string SslNumber { get; set; }  // Número SL del sistema legacy
}

public class SslInfo
{
    public bool Required { get; set; }      // ¿Requiere número SL?
    public bool Used { get; set; }          // ¿Ya se usó el número SL?
    public string LegacySslNumber { get; set; }  // Número SL original
}

public class SslRequirementInfo
{
    // Información sobre si se requiere el número SL
}
```

---

## 🚫 **Lo que NO es**

### **Confusiones Comunes:**

❌ **NO es:** Certificado SSL/TLS  
❌ **NO es:** Conexión HTTPS  
❌ **NO es:** Protocolo de seguridad  
❌ **NO es:** Encriptación de comunicaciones

✅ **SÍ es:** Número de identificación de licencias del sistema anterior

---

## 🔍 **Preguntas Frecuentes**

### **Q: ¿Por qué no se llama simplemente "SL" en el código?**

**A:** Por razones históricas. Cuando se diseñó la API, se usó "SSL" como abreviatura de "Serial License". Cambiar el nombre ahora requeriría modificar la API y todos los clientes, causando incompatibilidad.

### **Q: ¿Dónde obtiene el usuario su número SL?**

**A:** El número SL aparece en:

- Factura original del sistema antiguo
- Documento de licencia impreso
- Email de confirmación de compra original
- Sistema de soporte de WinCaja

### **Q: ¿Qué pasa si el usuario perdió su número SL?**

**A:** Con la nueva API v1.1:

- Solo se necesita para la **primera activación**
- Si ya activó una vez, puede reactivar sin el número SL
- Si nunca activó, debe contactar soporte para recuperarlo

### **Q: ¿Las licencias nuevas tienen número SL?**

**A:** No. Solo las **licencias migradas** del sistema legacy tienen número SL asociado.

---

## 📚 **Para Desarrolladores**

### **Al Leer el Código:**

Cuando veas `ssl`, `sslNumber`, `SslInfo`, etc.:

```csharp
// Piensa en:
"Número de Serie de Licencia del sistema antiguo"

// NO pienses en:
"Secure Sockets Layer" ❌
```

### **Al Documentar:**

**Preferir:**

- "Número SL"
- "Serial License"
- "Identificador del sistema legacy"

**Evitar:**

- "SSL" sin contexto (puede confundir)
- "Certificado SSL"
- "Protocolo SSL"

---

## ✅ **Resumen**

| Término          | Significado en este Proyecto                     |
| ---------------- | ------------------------------------------------ |
| **SSL**          | Security Lock / Serial License (número del chip) |
| **SslNumber**    | Número del chip físico (ej: SL11A13197)          |
| **SslInfo**      | Información del número SL                        |
| **ssl.used**     | ¿Ya se validó el número SL?                      |
| **ssl.required** | ¿Esta licencia requiere número SL?               |
| **licNoChip**    | Campo en BD histórica (sistema legacy)           |

---

**Fecha:** 14 de octubre de 2025  
**Versión API:** v1.1  
**Autor:** Equipo WinCaja License Manager
