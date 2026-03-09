# Antigravity Unity Extension

Unity development support for **Antigravity IDE** — debugger, syntax highlighting, IntelliSense, and project tools.

## Features

### Syntax Highlighting
- **ShaderLab** (`.shader`) — Full ShaderLab + embedded HLSL/CG support
- **HLSL/CG** (`.hlsl`, `.cginc`, `.cg`, `.compute`) — Types, functions, semantics
- **USS** (`.uss`) — Unity Style Sheets with UI Toolkit elements
- **UXML** (`.uxml`) — Unity XML with element and attribute highlighting
- **Assembly Definition** (`.asmdef`, `.asmref`) — JSON with asmdef keys

### Unity Debugger
- **Attach Unity Debugger** — Discovers Unity instances on network
- Supports Unity Editor and standalone Players
- Configurable debug bridge port
- Auto-generated `launch.json` configurations

### IntelliSense
- **50+ Unity API Messages** — `Start`, `Update`, `OnCollisionEnter`, etc.
- Method signatures with documentation
- Hover tooltips linking to Unity docs
- Smart context detection (only inside MonoBehaviour classes)

### Snippets (25+)
| Prefix | Description |
|---|---|
| `mono` | MonoBehaviour class |
| `scriptobj` | ScriptableObject with CreateAssetMenu |
| `editor` | Custom Editor |
| `editorwindow` | EditorWindow with MenuItem |
| `singleton` | Singleton pattern |
| `coroutine` | Coroutine method |
| `sfield` | [SerializeField] field |
| `dlog` | Debug.Log |
| `getcomp` | GetComponent<T>() |
| `instantiate` | Instantiate prefab |
| …and more |

### Commands
- **Antigravity: Attach Unity Debugger** — Discover and attach
- **Antigravity: Unity API Reference** — Open docs for selected symbol
- **Antigravity: Regenerate Project Files** — Trigger .csproj regeneration

## Building

```bash
npm install
npm run compile
```

## Requirements
- Antigravity IDE (VS Code-compatible)
- Unity 2021.3+ with Antigravity IDE Support package installed
