# Antigravity Unity — C# IntelliSense for Unity without Microsoft Lock-in

## The Problem

Microsoft's official **C#**, **C# Dev Kit**, and **Unity** extensions are **licensed exclusively for Visual Studio Code**. If you're using Antigravity IDE, VSCodium, or any other VS Code fork — you simply cannot install them. No IntelliSense, no debugging, no go-to-definition. You're stuck with a fancy text editor.

## The Solution

**Antigravity Unity** pairs with [DotRush](https://open-vsx.org/extension/nromanov/dotrush) — an open-source, MIT-licensed C# language server built on Roslyn — to give you **full C# IntelliSense, debugging, and Unity integration** without depending on Microsoft's proprietary extensions.

On top of that, we ship a custom Unity Editor package that optimizes `.csproj` generation. Instead of loading 150+ project files (most of them read-only UPM internals), we only generate the ~10-15 files you actually edit. Result: your project loads in **2-5 seconds** instead of minutes.

---

## ⚠️ Requirements

### 1. Unity Editor Package (Required)

A small Unity package that generates optimized project files for fast IntelliSense.

**Install via Unity Package Manager:**
1. Open Unity → **Window → Package Manager**
2. Click **"+" → Add package from git URL...**
3. Paste: `https://github.com/billythekidz/UnityAntigravityIDE.git`

**Then configure Unity:**
1. Go to **Edit → Preferences → External Tools**
2. Set **External Script Editor** to **Antigravity IDE** (or Visual Studio Code)
3. Click **"Regenerate project files"**

### 2. DotRush (Required — C# IntelliSense Engine)

Since Microsoft's C# extension isn't available, **DotRush is what gives you IntelliSense**. Without it, you won't have autocomplete, error checking, or go-to-definition.

- **Option A (Marketplace):** Search **"DotRush"** in Extensions and install `nromanov.dotrush`.
- **Option B (Manual VSIX):** Download from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) and install via "Install from VSIX...".

![DotRush Installation Guide](https://raw.githubusercontent.com/billythekidz/UnityAntigravityIDE/main/antigravity-unity-extension~/assets/dotrush_guide.jpg)

---

## 🚀 Quick Start

1. **Install this extension** from the Marketplace or [Open VSX](https://open-vsx.org/extension/antigravity-unity/antigravity-unity).
2. **Install the [Unity package](https://github.com/billythekidz/UnityAntigravityIDE.git)** via Package Manager.
3. Open your project in **Antigravity IDE**. If prompted, allow DotRush to install.
4. In Unity, set **Antigravity IDE** as your External Script Editor → click **"Regenerate project files"**.

> [!IMPORTANT]
> When prompted to select a solution file, always **choose the `.sln` file** (not `.csproj` or `.slnx`). This ensures full cross-project navigation and DotRush compatibility.

5. Done. IntelliSense should be working. If not, run `Developer: Reload Window`.

---

## ✨ What You Get

### C# IntelliSense (via DotRush + Roslyn)
- Full **C# 9.0+** autocomplete, go-to-definition, find references, real-time errors
- Works with all Unity assemblies — `UnityEngine`, `UnityEngine.UI`, `TextMeshPro`, `Netcode`, etc.
- Fast startup: our optimized `.csproj` generator means DotRush only parses your actual source code

### Unity Debugger
- **Attach to Unity Editor** — auto-discover running instances
- Breakpoints, variable inspection, call stacks
- Works with Editor and Standalone Players
- Auto-generated `launch.json`

### Syntax Highlighting
- **ShaderLab** (`.shader`) — full ShaderLab blocks + embedded CGPROGRAM/HLSLPROGRAM
- **HLSL/CG** (`.hlsl`, `.cginc`, `.cg`, `.compute`)
- **USS** (`.uss`) — Unity Style Sheets
- **UXML** (`.uxml`) — Unity XML
- **AsmDef** (`.asmdef`, `.asmref`) — Assembly Definitions

### 50+ Unity API Completions
Scaffold Unity event functions with correct signatures:
`Start`, `Update`, `Awake`, `FixedUpdate`, `OnCollisionEnter`, `OnTriggerEnter`, `OnValidate`, etc.

### 25+ C# Snippets
| Snippet | Output |
|---|---|
| `mono` | `MonoBehaviour` class |
| `scriptobj` | `ScriptableObject` with `[CreateAssetMenu]` |
| `editor` | `CustomEditor` class |
| `editorwindow` | `EditorWindow` with `[MenuItem]` |
| `singleton` | Thread-safe generic Singleton |
| `coroutine` | `IEnumerator` coroutine |
| `sfield` | `[SerializeField] private` field |

---

## 🏗️ Open Source

- **Repository**: [github.com/billythekidz/UnityAntigravityIDE](https://github.com/billythekidz/UnityAntigravityIDE)
- **Issues & Requests**: [GitHub Issues](https://github.com/billythekidz/UnityAntigravityIDE/issues)
- **License**: MIT

*Keywords: unity, c# intellisense, antigravity ide, vscodium, dotrush, roslyn, open source, microsoft c# alternative, unity debug*
