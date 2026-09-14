# Documentacion Humana

Esta carpeta contiene documentacion para usuarios, despliegue y operacion humana.

## Estructura

- `user/`: guias para usuario final o capacitacion.
- `deployment/`: publicacion, checklist y notas de configuracion.
- `AGENT_WORKFLOW.md`: flujo operativo vigente de OpenCode, permisos, propiedad temporal, handoffs y evidencia.
- `GENTLE_AI_ARCHITECTURE.md`: referencia historica de Gentle AI, SDD, OpenCode, Engram y CodeGraph; no sustituye el flujo operativo vigente.
- `README_CORRECCION_IMPORTACION_EXCEL.md`: guia de correccion manual del Excel historico y comparacion antes de importar.
- `GUIA_IMPORTACION_HISTORICA.md`: contrato Excel-modelo, reglas de
  importacion y deudas de calidad de los datos historicos.
- `GUIA_ENTREGA_PLANILLA_CLIENTE.md`: instrucciones de entrega, prioridades,
  recepcion, conciliacion y condiciones para promover respuestas de la
  cliente hacia QA y la base oficial.
- `PLAN_NORMALIZACION_HISTORICA_V2.md`: arquitectura de datos V2, prioridades
  P0/P1/P2, responsables, puertas de calidad y secuencia de implantacion.
- `MIGRACION_SQLSERVER_NUBE_A_LOCAL.md`: procedimiento seguro para traer una
  base remota a una copia local y validarla antes de promoverla.
- `OPERACION_BASE_OFICIAL_LOCAL.md`: base oficial, QA, acceso de minimo
  privilegio, bootstrap del administrador, respaldos y recuperacion.

## Seguridad

No guardar credenciales reales en esta carpeta. Usar placeholders y variables de entorno.

Ejemplos permitidos:

- `[DB_HOST]`.
- `[DB_USER]`.
- `[DB_PASSWORD]`.
- `[FTP_HOST]`.
- `[FTP_USER]`.
- `[FTP_PASSWORD]`.
