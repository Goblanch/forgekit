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

## 2026-09-20 - Parser híbrido: YamlDotNet para `.meta`, regex para grafo de referencias

**Decisión:** `UnityYamlParser.ParseMeta` deserializa `.meta` con YamlDotNet (paquete NuGet) contra un modelo tipado (`UnityMetaDocument`, con `[YamlMember(Alias = ...)]` por campo, porque Unity mezcla `guid` en minúscula con `TextureImporter` en PascalCase sin convencón consistente). `UnityYamlParser.ExtractReferencedGuids` usa una regex compilada (`[GeneratedRegex]`) sobre `.unity`/`.prefab`/`.mat`, buscando el patrón `` guid:\s*([0-9a-fA-F]{32})`, en vez de deserializar esos archivos como YAML tipado.

**Por qué:** los `.meta` son YAML de un único documento y estructura relativamente estable -> tiene sentido tipar. Los `.unity`/`.prefab` son multi-documento con tags custom (`!u!114 &...`) que no son YAML estándar y romperían un deserializador tipado sin trabajo adicional significativo - el regex es más simple y hace exactamente lo que el dominio necesita (extraer GUIDs referenciados), sin intentar modelar la semántica completa del formato de escena de Unity.

**Edge case documentado - Referencias "colgantes" a recursos built-in de Unity:** `HeroMaterial.mat` referencia un shader con `guid: 0000000000000000f000000000000000`, el GUID real que usa Unity para sus recursos internos (shaders por defecto, etc.), que no vive en `Assets/` ni tiene `.meta` en el proyecto. El regex lo captura igual que cualquier otro GUID; al construir el grafo de referencias, este GUID simplemente no coincidirá con ningún `AssetRecord` conocido. No es un bug - es una referencia legítima a algo fuera del proyecto, y el grafo debe tolerarla sin fallar (no lanzar excepción por un GUID "no encontrado" al resolver referencias).

---

## 2026-10-08 - Grafo de referencias y alcanzabilidad

**Decisión**: `AsssetScanner` ignora assets sin `.meta` (sin GUID no se pueden situar en el grafo). `ReferenceGraph.ReachableFrom` hace BFS desde las escenas con `enabled: 1` en `EditorBuildSettings.asset`, e incluye las propias raíces en el resultado. Las referencias a GUIDs sin `AssetRecords` (recursos built-in de Unity) se añaden al grafo igualmente y no provocan error.

**Por qué**: un asset sin `.meta` es un problema distinto (merge mal resuelto, ver SPEC 8.4) y no debe contaminar el análisis de huérfanos. Tolerar GUIDs desconocidos evita falsos errores con shaders y materiales por defecto.

**Nota de fixtures**: `icon_compressed.png` e `icon_compressed_old.png` aparecen como huérfanos junto a `OrphanDebugMarker.prefab` porque nada los referencia; el test lo refleja de forma explícita.

## 2026-10-08 - Capa de reglas: IAuditRule + AuditContext

**Decisión**: cada regla del SPEC 5 es una clase que implementa `IAuditRule` y recibe un `AuditContext` ya procesado (assets, alcanzables, texturas, opciones). `AssetAuditor` orquesta scan -> grafo -> reglas -> informe y ordena los hallazgos de forma determinista (severidad desc, categoría, ruta ordinal).

**Por qué**: las reglas no tocan disco, así que se testean con contextos construidos a mano (SPEC 7), y añadir una regla nueva no obliga a modificar las existentes. `Forgekit.Cli` solo tendrá que construir `AuditOptions` y llamar a `AssetAuditor.Run`.

**Matices respecto al SPEC**:

- `DuplicateRule` ignora archivos de 0 bytes (todos comparten hash y darían un falso duplicado).
- Las exclusiones por defecto son fragmentos de ruta (`/Resources/`, `/Addressables/`); el soporte de globs para `--exclude` llega con el CLI.
- Los `.meta` de texturas se parsean dos veces (scanner y `TextureImportReader`); aceptado en v1 por simplicidad.

---

## 2026-10-13

**Decisión**: `JsonReportFormatter` serializa DTOs privados, no el dominio, con `System.Text.Json` (camelCase, enums como texto, `UnsafeRelaxedJsonEscaping`). `MarkdownReportFormatter` agrupa por severidad desc + categoría, escapa `|` y usa solo `\n`. `AuditReport.CountOf(severity)` sustituye a `SummaryBySeverity` para los resúmenes, porque este omite las severidades con cero hallazgos.

**Por qué**: el JSON es un contrato para consumidores en CI; desacoplarlo del dominio evita cambios accidentales. El escape relajado mantiene legibles el `+` del timestamp y los caracteres no ASCII; no hay riesgo porque la salida no se incrusta en HTML. Los saltos `\n` fijos hacen el informe idéntico en cualquier SO.

**Política de dependencias**: `Forgekit.Core` se mantiene sin librerías de UI/consola (solo YamlDotNet para `.meta`). Cualquier dependencia de presentación irá en `Forgekit.Cli`.

**Deuda anotada**: `MarkdownReportFormatter` trata `DuplicateAsset` como caso especial para listar rutas; si aparecen más, añadir `RelatedPaths` a `Finding`.

---

## 2026-10-19 — CLI: capas finas, Spectre.Console en el borde, códigos de salida

**Decisión**: `Program.cs` → `AssetAuditCommand` (opciones de `System.CommandLine`) → `AssetAuditRunner` (validación, ejecución, emisión, código de salida). El runner recibe `IAnsiConsole` y dos `TextWriter`, sin depender de `System.CommandLine`. Spectre.Console solo existe en `Forgekit.Cli`; `Core` sigue sin librerías de presentación. Los errores de parseo se gestionan a mano para devolver 2 (el valor por defecto de `System.CommandLine` es 1, que en el SPEC §2.3 significa "hallazgos ≥ `--fail-on`").

**Matices respecto al SPEC**:

- `--exclude` acepta un fragmento de ruta (p. ej. `Assets/ThirdParty/`), no un glob. Evita una dependencia más (`Microsoft.Extensions.FileSystemGlobbing`) en v1.
- `--verbose` no se implementa todavía: requeriría notificación de progreso desde `Core`. No se acepta la opción para no prometer algo que no hace.
- `--fail-on` evalúa el informe completo, sin aplicar `--severity-threshold` (el filtro es solo de visualización).
- El texto de la consola está en inglés; el ejemplo del SPEC §6.1 estaba en español y debe actualizarse.
- `--format json|markdown` sin `--output` escribe a stdout; `--format console` con `--output` es un error de uso.
- El proyecto se empaqueta como `dotnet tool` (`PackAsTool`, comando `forgekit`).

---

## 2026-10-20 — `--exclude` aplica a todas las reglas

**Decisión**: `--exclude` ignora los assets que coinciden en todas las reglas (huérfanos, texturas, duplicados). `/Resources/` y `/Addressables/` siguen siendo exclusiones internas, solo del chequeo de huérfanos. Los assets excluidos siguen formando parte del grafo de referencias.

**Alternativas consideradas**: dejar `--exclude` solo para huérfanos (descartada: la prueba manual mostró que se espera que ignore la carpeta entera, por ejemplo ThirdParty); filtrar antes de construir el grafo (descartada: un prefab excluido dejaría de mantener vivas sus texturas y saldrían falsos huérfanos).

**Por qué**: es lo que el usuario espera de una exclusión, y filtrar solo en el contexto de las reglas la hace correcta sin tocar el grafo.

## 2026-10-20 — Salida: `--output` validada y resumen sin filtrar

**Decisión**: `--output` exige una extensión que encaje con el formato (`.json`, `.md`/`.markdown`) y falla con código 2 antes de escribir. El resumen de consola cuenta todos los hallazgos e indica cuántos oculta `--severity-threshold`. Cuando `--fail-on` falla, se escribe el motivo en stderr.

**Alternativas consideradas**: pedir `--force` para sobrescribir (descartada: en CI sobrescribir el informe anterior es lo normal).

**Por qué**: la prueba manual sobrescribió un PNG con JSON sin avisar, y un resumen con "0 findings" junto a un exit code 1 era engañoso.
