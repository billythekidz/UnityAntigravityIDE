# Antigravity Unity — Fast, Lightweight Unity VS Code Extension

**Antigravity Unity** is the ultimate lightweight, high-performance alternative to the official Unity extensions for VS Code and VSCodium. Designed for speed, it drops bloat, fixes memory leaks, and provides a blazing-fast C# Unity development experience using the DotRush Roslyn engine.

Say goodbye to "OmniSharp server is not running" and 5-minute solution load times. Load your Unity project in **2-5 seconds** and get back to making games.

## ⚠️ Required: Antigravity IDE Support Unity Package

To achieve lightning-fast IntelliSense, this extension requires a tiny custom Unity Editor package that optimizes `.csproj` generation (cutting from 150+ project files down to ~10 user-editable ones).

### Install via Unity Package Manager
1. Open your Unity Editor
2. Go to **Window → Package Manager**
3. Click **"+" → Add package from git URL...**
4. Paste:
```
https://github.com/billythekidz/UnityAntigravityIDE.git
```

### Setup in Unity
1. Go to **Edit → Preferences → External Tools**
2. Set **External Script Editor** to **VS Code** (or Antigravity IDE/Cursor)
3. Click **"Regenerate project files"**

---

## ✨ Features for Unity Developers

### 🧠 C# IntelliSense (Powered by DotRush Roslyn)
- Full **C# 9.0+ IntelliSense**, autocomplete, go-to-definition, and real-time error checking
- Supports all core Unity assemblies (`UnityEngine`, `UnityEngine.UI`, `Unity.Netcode`, `TextMeshPro`)
- **Auto-installs [DotRush](https://marketplace.visualstudio.com/items?itemName=nromanov.dotrush)** — a lightweight cross-platform C# extension
- **Blazing Fast Startup**: Our custom generator bypasses compiling internal Unity UPM packages. DotRush only parses your actual source code.

### 🐛 Unity Debugger
- **Attach to Unity Local Editor** — Discover and attach instantly
- Breakpoints, variable inspection, and call stacks for both Editor and Standalone Players
- Auto-generated `launch.json` setup

### 🎨 Complete Syntax Highlighting
- **ShaderLab** (`.shader`) — Full support for ShaderLab blocks + embedded CGPROGRAM/HLSLPROGRAM
- **HLSL/CG** (`.hlsl`, `.cginc`, `.cg`, `.compute`) — Types, vector maths, semantics
- **USS** (`.uss`) — Unity Style Sheets with UI Toolkit elements
- **UXML** (`.uxml`) — Unity XML with custom element highlighting
- **AsmDef** (`.asmdef`, `.asmref`) — Assembly Definitions JSON with special keys

### � 50+ Unity API Auto-Completions
Instantly scaffold Unity event functions with proper signatures and summaries inside `MonoBehaviour`:
- `Start`, `Update`, `Awake`, `FixedUpdate`, `LateUpdate`
- `OnCollisionEnter`, `OnTriggerEnter`, `OnEnable`, `OnValidate`

### 📝 25+ Unity C# Snippets
| Snippet | Output |
|---|---|
| `mono` | Basic `MonoBehaviour` class |
| `scriptobj` | `ScriptableObject` with `[CreateAssetMenu]` |
| `editor` | `CustomEditor` class structure |
| `editorwindow` | `EditorWindow` with `[MenuItem]` |
| `singleton` | Thread-safe generic Singleton |
| `coroutine` | `IEnumerator` coroutine method |
| `sfield` | `[SerializeField] private Type fieldName;` |

---

## 🚀 Quick Start Guide

1. **Install this extension** from Open VSX / VS Code Marketplace
2. **Install the [Unity package](https://github.com/billythekidz/UnityAntigravityIDE.git)** via Package Manager 
3. Open your project in VS Code. If prompted, **allow DotRush to install**.
4. In Unity, set VS Code as your External Script Editor and click **"Regenerate project files"**.
5. Switch back to VS Code. Enjoy instant IntelliSense and zero OmniSharp crashes! 🎉

---

## 🏗️ Open Source & GitHub Links

- **Repository**: [github.com/billythekidz/UnityAntigravityIDE](https://github.com/billythekidz/UnityAntigravityIDE)
- **Bug Reports & Feature Requests**: [GitHub Issues](https://github.com/billythekidz/UnityAntigravityIDE/issues)
- **License**: MIT

*Keywords: unity, unity3d, unity extensions, vs code unity, c# intellisense, unity debug, custom editor, dotrush, roslyn, omnisharp alternative*
