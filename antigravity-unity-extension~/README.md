# Antigravity Unity — C# IntelliSense for Unity without Microsoft Lock-in

[![Open VSX Version](https://img.shields.io/open-vsx/v/antigravity-unity/antigravity-unity?label=Open%20VSX&color=blueviolet)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity)
[![Open VSX Downloads](https://img.shields.io/open-vsx/dt/antigravity-unity/antigravity-unity?color=brightgreen)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity)
[![Open VSX Rating](https://img.shields.io/open-vsx/rating/antigravity-unity/antigravity-unity?color=orange)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity#review-details)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## The Problem

Microsoft's official **C#**, **C# Dev Kit**, and **Unity** extensions are **licensed exclusively for Visual Studio Code**. If you're using Antigravity IDE, VSCodium, or any other VS Code fork — you simply cannot install them. No IntelliSense, no debugging, no go-to-definition. You're stuck with a fancy text editor.

## The Solution

**Antigravity Unity** pairs with [DotRush](https://open-vsx.org/extension/nromanov/dotrush) — an open-source, MIT-licensed C# language server built on Roslyn — to give you **full C# IntelliSense, debugging, and Unity integration** without depending on Microsoft's proprietary extensions.

On top of that, we ship a custom Unity Editor package that optimizes `.csproj` generation. Instead of loading 150+ project files (most of them read-only UPM internals), we only generate the ~10-15 files you actually edit. Result: your project loads in **2-5 seconds** instead of minutes.

---

## ⚠️ Requirements

### 1. DotRush (Required — C# IntelliSense Engine)

Since Microsoft's C# extension isn't available, **DotRush is what gives you IntelliSense**. Without it, you won't have autocomplete, error checking, or go-to-definition.

- **Option A (Marketplace):** Search **"DotRush"** in Extensions and install `nromanov.dotrush`.
- **Option B (Manual VSIX):** Download from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) and install via "Install from VSIX...".

![DotRush Installation Guide](https://raw.githubusercontent.com/billythekidz/UnityAntigravityIDE/main/antigravity-unity-extension~/assets/dotrush_guide.jpg)

### 2. Unity Editor Package (Required)

A small Unity package that generates optimized project files for fast IntelliSense.

**Install via Unity Package Manager:**
1. Open Unity → **Window → Package Manager**
2. Click **"+" → Add package from git URL...**
3. Paste: `https://github.com/billythekidz/UnityAntigravityIDE.git`

**Then configure Unity:**
1. Go to **Edit → Preferences → External Tools**
2. Set **External Script Editor** to **Antigravity IDE** (or Visual Studio Code)
3. Click **"Regenerate project files"**

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

<a href='https://ko-fi.com/Y8Y61ABMM' target='_blank'>
  <img width='400' src='https://storage.ko-fi.com/cdn/kofi2.png?v=6' alt='Support me on Ko-fi' />
</a>

## ✨ What You Get

### C# IntelliSense (via DotRush + Roslyn)
- Full **C# 9.0+** autocomplete, go-to-definition, find references, real-time errors
- Works with all Unity assemblies — `UnityEngine`, `UnityEngine.UI`, `TextMeshPro`, `Netcode`, etc.
- Fast startup: our optimized `.csproj` generator means DotRush only parses your actual source code

### Unity Debugger ⚠️ *Experimental*
- **Attach to Unity Editor** — auto-discover running instances
- Breakpoints, variable inspection, call stacks
- Auto-generated `launch.json`

> **Note:** The debugger is still a work in progress. Basic breakpoints and variable inspection work, but some advanced features (conditional breakpoints, Edit & Continue) are not yet fully supported. We're actively improving this.

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

## ❓ FAQ / Troubleshooting

### IntelliSense not working — no autocomplete, no error checking

This is the #1 reported issue. In almost every case, it's one of these:

| Check | Fix |
|-------|-----|
| DotRush not installed | Install `nromanov.dotrush` from Extensions or [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) |
| Wrong solution file selected | When DotRush prompts, pick the **`.sln`** file — not `.csproj` or `.slnx` |
| No `.sln` file exists | In Unity: **Edit → Preferences → External Tools → Regenerate project files** |
| DotRush not activated | Check the Extensions panel — DotRush must be **enabled**, not just installed |

After fixing any of the above, run `Ctrl+Shift+P` → `Developer: Reload Window`.

### Special characters in folder/project names (`&`, `+`, `#`, etc.)

If any folder in your Unity project path contains special characters like `&`, `+`, `#`, or non-ASCII characters, DotRush/Roslyn may fail to parse `.csproj` files. **Rename the folder** to use only alphanumeric characters, dashes, and underscores.

### "Cannot find C# extension" or "OmniSharp not found"

This is expected. Microsoft's C# and C# Dev Kit extensions **cannot be installed** on Antigravity IDE or VSCodium due to licensing restrictions. That's exactly why this extension exists — we use **DotRush** instead of OmniSharp/C# Dev Kit.

If you see references to OmniSharp in error messages, you can safely ignore them. Just make sure DotRush is installed.

### Windows: `.NET SDK not found` or `dotnet` command errors

DotRush requires the .NET SDK to be installed and accessible:

1. Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download) (SDK, not just Runtime).
2. After installation, **restart Antigravity IDE** (not just reload).
3. Verify in terminal: `dotnet --version` should return a version number.

### Windows: Long path errors (`MAX_PATH` exceeded)

Unity projects nested deep in folders can hit Windows' 260-character path limit:

1. Open **Registry Editor** → `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem`
2. Set `LongPathsEnabled` to `1`
3. Or run in an elevated PowerShell: `New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force`
4. Restart your machine.

### Windows: Firewall blocks DotRush or extension downloads

Some corporate environments block Open VSX or GitHub:

- Try downloading the `.vsix` files manually from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) or [GitHub Releases](https://github.com/billythekidz/UnityAntigravityIDE/releases/latest).
- Install via `Ctrl+Shift+P` → `Extensions: Install from VSIX...`.

### Windows: `Unable to watch for file changes` error

This happens when your Unity project has too many files for Windows' file watcher:

- Add `"files.watcherExclude"` to your workspace settings:
```json
{
  "files.watcherExclude": {
    "**/Library/**": true,
    "**/Temp/**": true,
    "**/obj/**": true
  }
}
```

### Debugging: "Cannot attach to Unity" or no Unity instances found

1. Make sure Unity Editor is **running** with your project open.
2. In Antigravity IDE: `Ctrl+Shift+P` → `Antigravity: Attach Unity Debugger`.
3. If no instances appear, check that Unity's **Script Debugging** is enabled in Build Settings.
4. On Windows, ensure your firewall isn't blocking the debugger port.

### Unity shows "Antigravity (internal)" instead of the real editor name

Your Unity project has **compile errors**. Fix all C# errors in the Console, then the dropdown in **Edit → Preferences → External Tools** will show "Antigravity" correctly.

---

## 🏗️ Open Source

- **Repository**: [github.com/billythekidz/UnityAntigravityIDE](https://github.com/billythekidz/UnityAntigravityIDE)
- **Issues & Requests**: [GitHub Issues](https://github.com/billythekidz/UnityAntigravityIDE/issues)
- **License**: MIT

*Keywords: unity, c# intellisense, antigravity ide, vscodium, dotrush, roslyn, open source, microsoft c# alternative, unity debug*
