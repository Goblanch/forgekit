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

---

## 2026-09-20 - Fixtures de tests generados por IA, no exportados de Unity

**Decisión:** los fixtures de `.unity`/`.prefab`/`.mat`/`.meta` en `tests/ForgeKit.Core.Tests/Fixtures/SampleUnityProject` están generados por IA reproduciendo la sintaxis real de Unity (YAML multi-documento, tags `!U!N &fileID`), en vez de exportrados desde el Editor de un proyecto real. Por el momento se usan estos Fixtures para testear. Más adelante, se obtendrán de un proyecto real.

**Por qué:** no había un proyecto de Unity disponible en el momento de escribir los tests. Los GUIDs se generaron programáticamente (por la IA) para garantizar 32 caracteres exáctamente.

**Riesgo aceptado y mitigación futura:** como el parser trabaja por regex sobre el patrón `guid: <hex32>`, estos fixtures son representativos para validar esa lógica de extracción sin depender de la semántica completa de Unity. En el futuro se dispondrá de un proyecto real de Unity y se sustituitrán los fixtures actuales.

**Escenario cubierto:** cadena de alcanzabilidad completa (Scene -> PrefabVariant -> PrefabBase -> Material -> Texture) vía `EditorBuildSettings.asset`, un asset verdaderamente huérfano (`OrphanDebugMarker.prefab`), una textura sin optimizar (4096px, sin comprimir) y una optimizada (128px, comprimida) como control negativo, y un duplicado por contenido binario ídéntico con distinto GUID.
