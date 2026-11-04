# Arquitectura: principios SOLID y patrones de diseño

**Proyecto:** {{proyecto}}
**Periodo:** Sprint {{N}} ({{dd/mm/aaaa}} → {{dd/mm/aaaa}})
**Stack:** {{stack}}

---

## 1. Contexto

{{Por qué existe el documento. Si hubo un diagrama anterior, decir qué cambió.}}

![Arquitectura por capas del sistema actual y dónde se aplica cada principio.](img/arquitectura.svg)

---

## 2. Capas

| Capa | Responsabilidad | Ejemplo real |
|---|---|---|
| {{capa}} | {{responsabilidad}} | `{{archivo}}` |

---

## 3. Cada principio, con su evidencia en el código

### S · Responsabilidad única
{{servicios y su única responsabilidad; tabla}}

### O · Abierto / cerrado
{{punto de extensión real + fragmento de código copiado del repo}}

### L · Sustitución de Liskov
{{interfaz con 2+ implementaciones intercambiables (producción/tests) + fragmento}}

### I · Segregación de interfaces
{{interfaces chicas por consumidor}}

### D · Inversión de dependencias + inyección
{{cuántos servicios se registran por interfaz (contar con grep) + extracto de Program.cs}}

---

## 4. Patrones de diseño

| Patrón | Dónde | Para qué |
|---|---|---|
| {{patrón}} | `{{clase}}` | {{beneficio}} |

---

## 5. Del diagrama anterior al sistema actual

| Diagrama anterior | Equivalente real hoy |
|---|---|
| {{elemento}} | {{clase real}} |

---

## 6. Límites (transparencia)

- {{Dónde NO se aplica el principio y por qué; con cifras, p. ej. «139 de 153 PageModels usan EF Core directamente»}}
