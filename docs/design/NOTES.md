# Decisiones de diseño

Registro de decisiones de diseño relevantes.
Formato por entrada: fecha, decisión, alternativas consideradas, por qué se descartan.

---

## 2026-09-19 - Nombre y scope del proyecto

**Decisión:** el proyecto se llama `forgekit` y se diseña como CLI con subcomandos (`forgekit assetaudit`, y más adelante `forgekit changelog`), en vez de repos independientes por hearramientas.

**Alternativas consideradas:** `assetguard` (nombre específico, solo para el auditor) -> descartado porque el proyecto agrupa varias herramientas bajo el mismo CLI.

**Por qué:** un CLI único con arquitectura de subcomandos demuestra diseño de herramientas extensibles (mismo patrón que git), más valioso en portfolio que dos scripts sueltos.

## 2026-09-19 - Scope del auditor: Unity, no Unreal

**Decisión:** `assetaudit` solo soporta proyectos para Unity.

**Por qué:** los archivos de proyecto de Unity (`.unity`, `.prefab`, `.meta`) son YAML en texto plano, parseables desde fuera del Editor. Los `.uassset` de Unreal son binarios propietarios. Auditarlos sin las APIs de C++ del motor está fuera del scope para un proyecto de este tamaño.

## 2026-09-19 - Exclusión de Resources/ y Addressables/ del chequeo de huérfanos

**Decisión:** cualquier asset bajo convención de carpeta `Resources/` o `Addressables/` queda excluido por defecto del chequeo de "no usado".

**Por qué:** estos assets se cargan directamente por string en runtime (`Resources.Load("ruta"))`, no por referencia serializada, así que no aparecen en el grafo de GUIDs y darían falsos positivos sistemáticos. Documentado como limitación conocida en vez de intentar resolverlo en v1, ya que requeriría analizar código C#, no solo YAML).
