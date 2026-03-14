# Antigravity Unity — Fast, Lightweight Unity IDE Support

**Antigravity Unity** is the ultimate lightweight, high-performance extension for **Antigravity IDE**. Designed for speed, it drops bloat, fixes memory leaks, and provides a blazing-fast C# Unity development experience using the DotRush Roslyn engine.

Say goodbye to "OmniSharp server is not running" and 5-minute solution load times. Load your Unity project in **2-5 seconds** and get back to making games.

---

## ⚠️ Requirements

### 1. Antigravity IDE Support Unity Package (Required)

To achieve lightning-fast IntelliSense, this extension requires a tiny custom Unity Editor package that optimizes `.csproj` generation (cutting from 150+ project files down to ~10 user-editable ones).

#### Install via Unity Package Manager
1. Open your Unity Editor
2. Go to **Window → Package Manager**
3. Click **"+" → Add package from git URL...**
4. Paste: `https://github.com/billythekidz/UnityAntigravityIDE.git`

#### Setup in Unity
1. Go to **Edit → Preferences → External Tools**
2. Set **External Script Editor** to **Antigravity IDE** (or Visual Studio Code)
3. Click **"Regenerate project files"**

### 2. DotRush (C# IntelliSense & Debugging)

**DotRush is MANDATORY** for C# IntelliSense and debugging. While our extension tries to install it automatically, you **must** ensure it is installed:

- **Option A (Marketplace):** Search for **"DotRush"** in the Extensions view and install `nromanov.dotrush`.
- **Option B (Manual VSIX):** If you are on VSCodium or an offline environment, download the VSIX from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) and install it manually via "Install from VSIX...".

![DotRush Installation Guide](https://raw.githubusercontent.com/billythekidz/UnityAntigravityIDE/main/antigravity-unity-extension~/assets/dotrush_guide.jpg)

---

## 🚀 Quick Start Guide

1. **Install this extension** from the Antigravity IDE Extensions Marketplace (or Open VSX).
2. **Install the [Unity package](https://github.com/billythekidz/UnityAntigravityIDE.git)** via Package Manager. 
3. Open your project in **Antigravity IDE**. If prompted, **allow DotRush to install**.
4. In Unity, set **Antigravity IDE** as your External Script Editor and click **"Regenerate project files"**.
5. Switch back to **Antigravity IDE**. 

> [!IMPORTANT]
> **Pro Tip**: When prompted to select a solution file, always **prioritize choosing the `.sln` file** (not individual `.csproj` `.slnx` files) to ensure full DotRush compatibility and cross-project navigation.

6. Done! Enjoy instant IntelliSense and zero OmniSharp crashes! 🎉

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

### ⚡ 50+ Unity API Auto-Completions
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

## 🏗️ Open Source & GitHub Links

- **Repository**: [github.com/billythekidz/UnityAntigravityIDE](https://github.com/billythekidz/UnityAntigravityIDE)
- **Bug Reports & Feature Requests**: [GitHub Issues](https://github.com/billythekidz/UnityAntigravityIDE/issues)
- **License**: MIT

*Keywords: unity, unity3d, unity extensions, antigravity ide, c# intellisense, unity debug, custom editor, dotrush, roslyn, omnisharp alternative*
