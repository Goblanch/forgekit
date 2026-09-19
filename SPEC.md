# forgekit - Especificación técnica (SRS)

---

## 1. Propósito y alcance

### 1.1 Propósito

Definir con precisión suficiente para implementación qué hace `forgekit assetaudit`, cómo se invoca, qué datos de entrada consume, qué reglas aplica, y qué salidas produce.

### Alcance de versión v1

Dentro de alcance:

* Proyectos de **Unity** (versión 2021 LTS o superior, formato de escena/prefab en YAML, no binario).
* Detección de assets huérfanos por grafo de referencias GUID.
* Auditoría de configuración de importación de texturas.
* Detección de duplicados por contenido.
* Reporte en consola (tabla) y exportación a JSON/Markdown.

Fuera de alcance:

* Assets referenciados solo por código C# (`Resources.Load`, Addressables) - ver 5.4, limitación conocida.
* Corrección automática de los hallazgos (la herramienta es de diagnóstico, no de modificación del proyecto)
* Integración con el Editor de Unity (no es un Editor Extension, se ejecuta como proceso externo).

### 1.3 Audiencia

Desarrolladores individuales o equipos pequeños de Unity que quieran auditar su proyecto localmente o integrado en un pipeline de CI.

---

## 2. Interfaz de línea de comandos

### 2.1 Invocación base

```shell
forgekit assetaudit --project <ruta> [opciones]
```

### 2.2 Argumentos y opciones

| Argumento | Obligatorio | Tipo | Descripción |
| --- | --- | --- | --- |
| `--project <ruta>` | Sí | string (ruta) | Ruta a la raíz del proyecto Unity (la carpeta que contiene `Assets/` y `ProjectSettings/`). |
| `--output <ruta>` | No | string (ruta) | Ruta del archivo de reporte a generar. Si se omite, solo se imprime por consola. |
| `--format <formato>` | No | `console` \| `json` \| `markdown` | Formato de salida. Por defecto `console`. Si se especifica `--output` sin `--format`, se infiere del extensión del archivo. |
| `--max-texture-size <px>` | No | int | Umbral de tamaño máximo de textura antes de flagear como "sin optimizar". Por defecto `2048`. |
| `--exclude <patrón>` | No (repetible) | string (glob) | Rutas adicionales a excluir del análisis, además de las exclusiones por defecto (§5.4). Se puede pasar varias veces. |
| `--severity-threshold <nivel>` | No | `info` \| `warning` \| `error` | Solo reporta hallazgos igual o por encima de este nivel. Por defecto `info` (todo). |
| `--fail-on <nivel>` | No | `warning` \| `error` | Si se especifica, el proceso retorna código de salida distinto de 0 cuando existe al menos un hallazgo igual o por encima de ese nivel. Pensado para uso en CI. |
| `--verbose` | No | flag | Imprime progreso detallado del análisis (útil para debug, no para el reporte final). |

### 2.3 Códigos de salida

| Código | Significado |
| --- | --- |
| `0` | Análisis completado sin errores de ejecución. Puede haber hallazgos si no se suó `--fail-on`. |
| `1` | Análisis completado, pero hay hallazgos que igualan o superan el nivel de `--fail-on`. |
| `2` | Error de uso: ruta de proyecto inválida, argumentos incorrectos. |
| `3` | Error de ejecución: fallo de parseo irrecuperable en algún archivo del proyecto. |

### 2.4 Ejemplo de uso

```Shell
forgekit assetaudit --project ./MyGame --format markdown --output audit-report.md --max-texture-size 1024 --fail-on error
```

## 3. Modelo de datos de entrada

### 3.1 Estructura del proyecto Unity relevante

```txt
<raíz del proyecto>/
├── Assets/
│   ├── **/*.{prefab,unity,asset,mat,png,jpg,fbx,...}
│   └── **/*.meta          (uno por cada asset, mismo nombre + .meta)
├── ProjectSettings/
│   └── EditorBuildSettings.asset   (lista de escenas incluidas en el build)
```

### 3.2 Formato de archivos `.unity` / `.prefab` / `.asset`

YAML multi-documento (separdo por `--- !u!<classID> &<fileID>`). Cada referencia a otro asset tiene la forma:

```yaml
m_Material: {fileID: 2100000, guid: 8b2f1c4a9d3e4f5b8a1c2d3e4f5a6b7c, type: 2}
```

El campo relevante para el grafo de referencias es guid. Un mismo archivo puede contener múltiples referencias a múltiples GUIDs distintos.

### 3.3 Formato de archivos `.meta`

YAML de un único documento. Ejemplo relevante para texturas:

```yaml
fileFormatVersion: 2
guid: 8b2f1c4a9d3e4f5b8a1c2d3e4f5a6b7c
TestureImporter:
    maxTextureSize: 4096
    textureCompresion: 0    # 0 = sin comprimrir
```

Campos que la herramienta debería extraer:

* `guid` (siempre presente, identifica el asset).
* `TextureImporter.maxTextureSize` (solo en `.meta` de imágenes).
* `TextureImpoorter.textureCompression` (solo en `.meta` de imágenes. `0` = sin comprimir).

### 3.4 `EditorBuildSettings.asset`

Contiene la lista de escenas incluidas en la build activa, cada una como ruta + GUID + flag `enabled`. Las escenas listadas aquí se consideran "raíces" del grafo de referencias - todo lo alcanzable desde ellas no es huérfano.

---

## 4. Modelo de dominio (para diseño de clases)

| Entidad | Descripción | Campos clave |
| --- | --- | --- |
| `AssetRecord` | Un asset del proyecto | `Guid`, `Path`, `AssetType` (enum: Texture, Prefab, Scene, Material, ScriptableObject, Other), `FileSizeBytes`, `ContentHash` |
| `ReferenceEdge` | Una referencia de un asset a otro | `FromGuid`, `ToGuid` |
| `TextureImportInfo` | Config de importación de una textura | `Guid`, `MaxSize`, `CompressionEnabled` |
| `Finding` | Un hallazgo del auditor | `Category` (enum: OrphanAsset, UnoptimizedTexture, DuplicateAsset), `Severity` (Info/Warning/Error), `AssetPath`, `Message`, `Details` (dict libre) |
| `AuditReport` | Resultado completo de una ejecución | `ProjectPath`, `Timestamp`, `Findings: List<Finding>`, `Summary` (conteo por categoría/severidad) |

---

## 5. Algoritmos y reglas de negocio

### 5.1 Construcción del grafo de referencias

1. Anumerar todos los archivos bajo `Assets/` con extensión `.unity`, `.prefab`, `.asset`, `.mat`, `.controller` (los formatos YAML de Unity).
2. Para cada uno, parsear como YAML (multi-documento donde aplique) y extraer, vía regex o parser YAML tolerante a los tags custom de unity (`!u!...`), todas las apariciones de `guid: <hash>`.
3. El GUID del propio archivo (extraído de su `.meta` correspondiente) es el nodo origen; cada `guid` encontrado en su contenido es una arista saliente.
4. Resultado: grafo dirigido `Dictionary<Guid, List<Guid>>`

**Nota de implementación:** Unity no distingue en el YAML entre "esta es una referencia real" y "este GUID aparece por casualidad en un comentario o campo no-referencial" - en la prácitca, el patrón `guid: <hex32>` dentro de un bloque `{fileId: ..., guid: ..., type: ...}` es suficientemente específico como para no dar falsos positivos relevantes.

### 5.2 Detección de asstes huérfanos

1. Nodos raíz = GUIDs de las escenas listadas en `EditorBuildSettings.asset` con `enabled = 1`.
2. BFS/DFS desde los nodos raíz sobre el grafo de referencias -> conjunto de GUIDs alcanzables.
3. Cualquier `AssetRecord` cuyo GUID **no** esté en el conjunto alcanzable, **y** no esté bajo una ruta excluida (5.4), es un hallazgo `OrphanAsset` con severidad `Warning`.

### 5.3 Auditoría de importación de texturas

Para cada `TextureImportInfo`:

* Si `MaxSize > --max-texture-size` y `CompressionEnabled == false` -> hallazgo `UnoptimezedTexture`, severidad `Warning`.
* Si `MaxSize > --max-texture-size` y `CompressionEnabled == true` -> hallazgo `UnoptimizedTexture`, severidad `Info` (menos grave, ya está comprimida).

### 5.4 Exlusiones por defecto

Excluidos automáticamente del chequeo de huérfanos (no de los demás chequeos):

* Cualquier ruta bajo `Assets/**/Resources/`
* Cualquier ruta bajo `Assets/**/Addressables/` (o marcada como grupo de Addressables, si se detecta el archivo de config correspondiente).
* Rutas adicionales pasadas vía `--exclude`.

**Razón:** estos assets se cargan por string/ID en tiempo de ejecución, no por referencia serializada - no aparecen en el grafo aunque estén en uso activo. Ver `NOTES.md`, entrada 2026-09-19.

### 5.5 Detección de duplicados

1. Para cada `AssetRecord`, calcular `ContentHash` (SHA-256 del contenido binario del archivo, no del `.meta`).
2. Agrupar por `ContentHash`. Cualquier grupo con más de un `AssetRecord` es un hallazgo `DuplicateAsset`, severidad `Info`, listando todas las rutas implicadas.

### 5.6 Prefab variants (regla de exclusión de falso positivo)

Un prefab variant declara su base mediante `m_CorrespondingSourceObject` con un `guid` apuntando al prefab padre. Esa arista se añade al grafo igual que cualquier otra referencia - no requiere lógica especial más allá de que el parser la capture como cualquier otro campo con `guid`.

---

## 6. Especificación de reporte de salida

### 6.1 Formato consola

Tabla agrupada por categoría, ordenada por severidad descendente (Error -> Warning -> Info):

```text
forgekit assetaudit — MyGame
 
[ERROR]   (0 hallazgos)
[WARNING] OrphanAsset (3)
  Assets/Textures/old_logo_v2.png
  Assets/Prefabs/unused/DebugMarker.prefab
  Assets/Materials/test_mat.mat
[WARNING] UnoptimizedTexture (1)
  Assets/Textures/character_diffuse.png (4096px, sin comprimir)
[INFO]    DuplicateAsset (1 grupo)
  Assets/UI/icon_close.png == Assets/UI/Deprecated/icon_close_old.png
 
Resumen: 3 warnings, 1 info, 0 errors — 5 hallazgos totales sobre 842 assets analizados.
```

### 6.2 Formato JSON

```JSON
{
  "projectPath": "./MyGame",
  "timestamp": "2026-09-19T17:30:00Z",
  "summary": { "error": 0, "warning": 4, "info": 1, "totalAssetsScanned": 842 },
  "findings": [
    {
      "category": "OrphanAsset",
      "severity": "Warning",
      "assetPath": "Assets/Textures/old_logo_v2.png",
      "message": "Asset no referenciado por ninguna escena alcanzable desde Build Settings.",
      "details": { "guid": "8b2f1c4a9d3e4f5b8a1c2d3e4f5a6b7c" }
    }
  ]
}
```

### 6.3 Formato Markdown

Mismo contenido que consola, formateado como tabla Markdown, pensado para pegar en un PR o issue de GitHub.

---

## 7. Requisitos no funcionales

| Requisito | Detalle |
| --- | --- |
| **Rendimiento** | Debe analizar un proyecto de ~5.000 assets en menos de 30 segundos en hardware de desarrollo estándar. El parseo de archivos debe paralelizarse (no es un requisito de v1 bloqueante, pero condiciona el diseño para no serializar todo el I/O). |
| **Determinismo** | Misma entrada → mismo reporte, siempre (el orden de los hallazgos debe ser estable, no depender del orden de iteración del sistema de archivos). |
| **Sin dependencia del Editor de Unity** | Debe funcionar con el proyecto cerrado, sin Unity instalado en la máquina que ejecuta el análisis (requisito clave para uso en CI). |
| **Extensibilidad** | La arquitectura de comandos debe permitir añadir `forgekit changelog` como subcomando adicional sin reestructurar el punto de entrada. |
| **Testing** | Cada regla de negocio (§5) debe ser testeable de forma aislada contra fixtures de archivos `.unity`/`.meta` de ejemplo, sin depender de un proyecto Unity real. |

---

## 8. Limitaciones conocidas (v1)

Estas limitaciones se documentan aquí y se trasladan al README como "Limitaciones Conocidas":

1. Assets cargados dinámicamente por string (`Resources.Load`) o vía Addressables no se validan contra el grafo - se excluyen preventivamente (5.4), no se analizan.
2. No se distingue entre una escena en Build Settings deshabilitada (`enabled = 0`) que aun así se carga manualmente en runtime (`SceneManager.LoadScene`) - esas escenas no cuentan como raíz y todo lo que solo ellas referencian aparecerá como huérfano.
3. No se analiza código C# - cualquier referencia a un asset hecha solo desde código (fuera de los casos ya cubiertos) no se detecta.
4. Merges de Git mal resueltos que dejan `.meta` sin asset asociado, o assets sin `.meta` no tienen ninguna regla dedicada en v1 (se podría añadir como `Category.OrphanMeta en una iteración futura).

---

## 9. Glosario

* **GUID**: identificador único de 32 caracteres hexadecimales que Unity asigna a cada asset, almacenado en su `.meta`.
* **Asset huérfano**: asset no alcanzable por referencia desde ninguna escena incluida en build.
* **Prefab Variant**: prefab que hereda de otro prefab base, sobreescribiendo solo algunas propiedades.
* **Addressables**: sistema de Unity para cargar assets por dirección/ID en vez de por referencia directa, típicamente para contenido descargable o gestión de memoria.
