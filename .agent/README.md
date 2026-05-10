# .agent - Contexto Operativo

Esta carpeta contiene el conocimiento que debe usar un agente o desarrollador para trabajar rapido sin reconstruir contexto.

## Entrada Principal

1. `../context.md`: indice minimo desde la raiz del proyecto.
2. `.agent/context/00-router.md`: mapa de decision por tipo de tarea.
3. `.agent/context/01-global-rules.md`: reglas que aplican siempre.
4. `.agent/context/areas/*`: reglas por capa tecnica.
5. `.agent/context/modules/*`: reglas por modulo funcional.
6. `.agent/skills/*/SKILL.md`: instrucciones especializadas ejecutables.

## Que Va Aqui

- Contexto para IA y colaboradores tecnicos.
- Reglas vigentes del proyecto.
- Skills y workflows sin secretos.
- Historico tecnico archivado cuando ayuda a auditar decisiones.

## Que No Va Aqui

- Credenciales reales.
- Guias de usuario final.
- Pasos manuales de despliegue con passwords.
- Bitacoras largas mezcladas con reglas vigentes.

La documentacion humana vive en `docs/`. El material antiguo vive en `.agent/archive/`.
