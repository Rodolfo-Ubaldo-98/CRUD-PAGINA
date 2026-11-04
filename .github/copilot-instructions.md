# Copilot Instructions

## Directrices del proyecto
- Usar UTF-8 de forma consistente en archivos, respuestas/configuración web y textos para que los acentos y caracteres españoles se rendericen correctamente.

## Reglas de Auditoría en SQL Server
- En SQL Server, utilizar triggers de auditoría en la tabla Persona (TR_Persona_Insert/Update/Delete).
- Las inserciones deben usar SCOPE_IDENTITY() y nunca @@IDENTITY.
- No usar OUTPUT sin INTO en INSERT/UPDATE sobre Persona.
- Antes de cualquier operación que cambie estatus, aprobación o credenciales, ejecutar sp_set_session_context con IdUsuarioApp en la misma conexión.