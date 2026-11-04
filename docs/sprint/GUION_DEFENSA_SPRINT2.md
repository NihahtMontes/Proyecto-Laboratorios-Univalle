# Guion de defensa — Sprint 2 (privado, no va en los PDF)

## 1. El mensaje en una frase

> En el Sprint 1 empezamos a migrar a React + NestJS + PostgreSQL. Al medirla, la descartamos porque frenaba la entrega. En este sprint cargamos la base real completa del cliente y construimos el módulo de Planificación de punta a punta, probado con 402 tests y 3.950 comprobaciones automáticas. Y somos transparentes: el 71 % de las historias está hecho, el 12 % es parcial (casi siempre por datos que debe dar el cliente) y el 17 % queda pendiente.

---

## 2. Guion (6–7 minutos)

1. **(30 s) Sprint 1, en corto.** «Migramos Usuarios a NestJS. Al medir el resto, eran varios sprints sin entregar nada y otro hosting. Lo descartamos el 29/09; el código queda en la rama `migration/react`.»
2. **(1 min) Backlog.** Mostrar la tabla de resumen: 42 historias, 30 hechas, 5 parciales y 7 pendientes. «No todo está al 100 %, y les digo qué falta y por qué.»
3. **(1 min) Base de datos real.** «Inventario, historial de mantenimiento, solicitudes de 2026 y catálogo académico.» Correr en vivo:

   ```
   cd Tools/ExcelImport && python verify_v12_full.py
   ```

   Resultado esperado: «0 diferencias».
4. **(2–3 min) Demo de Planificación.**
   - Panel → Grilla → nueva planificación → choque de docente (cuenta regresiva) → Conflictos (recomendación) → Reprogramar.
   - Cierre de práctica, entrando con el rol restringido.
5. **(1 min) Pruebas.** «Las pruebas encontraron defectos reales:
   - procesos: de 71 fallas a 0;
   - usuarios: de 13 a 0;
   - maestros y catálogos: de 27 a 0;
   - además, un integrante del equipo hizo QA y corregimos sus observaciones.»
6. **(30 s) Arquitectura.** «SOLID no dependía de NestJS: está en el código que funciona.» Mostrar `IPlanningLock` y `IOccupancySource`.
7. **(30 s) Qué sigue.**
   - datos del cliente;
   - conflictos 2–4;
   - grupos 4–5;
   - respaldos automáticos;
   - mejora de UX.

---

## 3. Preguntas difíciles

| Pregunta | Respuesta |
|---|---|
| «¿El Sprint 1 fue tiempo perdido?» | «No. La limpieza de datos se reutilizó completa, y la forma de trabajar por fases con evidencia es la que usamos hoy. La migración nos dio el dato para decidir: cancelar a tiempo también es un resultado.» |
| «¿Por qué no lo vieron antes?» | «El costo real solo se ve migrando un módulo completo. Lo hicimos con Usuarios, lo medimos y decidimos.» |
| «¿Dónde está PostgreSQL?» | «Los datos se cargaron en SQL Server, que es lo que corre el hosting. Las restricciones e índices se hicieron igual, con 15 migraciones en este sprint.» |
| «¿La base está bien?» | «Es fiel al Excel: 44.743 celdas comparadas y 0 diferencias. Pero no está completa: el Excel no trae fechas de adquisición, capacidades ni existencias de la mayoría de los insumos. El sistema no inventa datos: los marca y tenemos la lista exacta para pedírsela al cliente.» |
| «¿Funciona la vida útil?» | «A medias. Cada equipo tiene sus años de vida útil cargados (121/121), pero ninguna unidad trae fecha de adquisición. En cuanto se cargue la fecha, el cálculo de vida restante ya está hecho y aparece solo.» |
| «¿Funciona el aforo?» | «La regla está hecha y probada, pero ningún ambiente tiene capacidad cargada. Se activa al registrarla.» |
| «¿Cómo sé que funciona?» | «Cuatro tipos de prueba: unitarias, HTTP automáticas, invariantes SQL y navegador. Hoy (26/10) volvimos a correr los 402 tests y la verificación contra el Excel.» |
| «¿Qué no funciona?» | Decirlo antes de que pregunten: la paginación de Mantenimientos (hallazgo abierto), el script general de Planificación sin adaptar, los respaldos automáticos y las correcciones sin commit. |
| «¿Quién hizo qué?» | Danielo hizo el circuito L-3/L-5 inicial (commit `ba0d1ae`) y QA; Fernando, los datos; Nihaht, la integración y el backend. **Confírmalo con el equipo antes.** |

---

## 4. Antes de presentar

- [ ] **Hacer commit en `qa` de las correcciones del 19 y 21/10**, para que lo que se defiende esté en GitHub.
  - Ojo: el árbol tiene más de 220 archivos modificados que mezclan esas correcciones con la mejora de UX.
  - El 26/10, a las 12:14, **otra sesión seguía editando** archivos de UX: `Pages/Ayuda`, `HelpContent.cs`, `validation-es.js`.
  - Hay que separarlas con cuidado.
- [ ] **No mostrar la ayuda ni el glosario nuevos** si la mejora de UX no está terminada y revisada.
- [ ] **Preparar la demo sobre una copia de la base**, no sobre la real: crear datos de demo deja rastro.
- [ ] **Confirmar con Fernando y Danielo** la autoría de cada parte.
- [ ] **Llevar los 4 PDF** de `outputs/presentacion-sprint2/`.
