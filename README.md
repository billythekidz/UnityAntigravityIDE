# Antigravity Unity IDE Support

[![Open VSX Version](https://img.shields.io/open-vsx/v/antigravity-unity/antigravity-unity?label=Open%20VSX&color=blueviolet)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity)
[![Open VSX Downloads](https://img.shields.io/open-vsx/dt/antigravity-unity/antigravity-unity?color=brightgreen)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity)
[![Open VSX Rating](https://img.shields.io/open-vsx/rating/antigravity-unity/antigravity-unity?color=orange)](https://open-vsx.org/extension/antigravity-unity/antigravity-unity#review-details)
[![GitHub Release](https://img.shields.io/github/v/release/billythekidz/UnityAntigravityIDE?color=blue)](https://github.com/billythekidz/UnityAntigravityIDE/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Full-featured Unity integration for Antigravity IDE — IntelliSense, debugging, Roslyn analyzers, and optimized project generation.

## ✨ Features

### 🎯 Smart Project Generation
- **Optimized for speed**: Only generates `.csproj` for user-editable assemblies (Assets/ + local Packages/), skipping read-only package internals. Typical Unity projects drop from ~155 to ~10-15 project files.
- **Auto-cleanup**: Removes orphaned `.csproj` and competing `.slnx` files automatically
- **DotRush-compatible references**: Emits both `<Reference>` with `<HintPath>` (for Roslyn type resolution) and `<ProjectReference>` (for IDE navigation)
- **Response file support**: Parses `.rsp` files for defines, references, and unsafe flags
- **Non-script assets**: Includes `.uxml`, `.uss`, `.shader`, `.asmdef` as `<None>` items for navigation

### 🧠 C# IntelliSense (via DotRush)
- Full IntelliSense, autocomplete, and error checking powered by Roslyn
- Supports all Unity assemblies including `UnityEngine.UI`, `TextMeshPro`, etc.
- Fast startup: filtered solution loads in seconds, not minutes
- Auto-install: prompts to install DotRush on first activation if not present

### 🎨 Syntax Highlighting
- **ShaderLab** (`.shader`)
- **HLSL/CG** (`.cginc`, `.hlsl`, `.cg`, `.compute`)
- **USS** — Unity Style Sheets (`.uss`)
- **UXML** — Unity XML (`.uxml`)
- **Assembly Definitions** (`.asmdef`, `.asmref`)

### 🔧 Unity Project Tools
- `Antigravity: Regenerate Project Files` — regenerate all `.csproj` and `.sln` from Unity
- `Antigravity: Attach Unity Debugger` — attach to running Unity instance *(experimental)*
- `Antigravity: Unity API Reference` — quick access to Unity docs
- Unity C# code snippets (MonoBehaviour methods, attributes, etc.)

---

## 📦 Installation

### Unity Package (required)
Add to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.antigravity.ide": "https://github.com/billythekidz/UnityAntigravityIDE.git"
  }
}
```

Or use Unity Package Manager → Add package from git URL:
```
https://github.com/billythekidz/UnityAntigravityIDE.git
```

### 1. Antigravity Unity IDE Extension
The extension provides Unity debugger, syntax highlighting for shaders, and deep IDE integration. 

- **Option A (Marketplace):** Search for **"Antigravity Unity"** in the Extensions Marketplace (or [Open VSX](https://open-vsx.org/extension/antigravity-unity/antigravity-unity)) and install.
- **Option B (Manual VSIX):** Download the latest `.vsix` from our [GitHub Releases](https://github.com/billythekidz/UnityAntigravityIDE/releases/latest) and install via `Extensions: Install from VSIX...`.

### 2. DotRush (Mandatory for IntelliSense)
**DotRush is REQUIRED** for C# IntelliSense and debugging.

- **Marketplace:** Search for **"DotRush"** or install `nromanov.dotrush`.
- **Manual install:** Download from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) if using an offline environment.

![Installation Guide](https://raw.githubusercontent.com/billythekidz/UnityAntigravityIDE/main/antigravity-unity-extension~/assets/dotrush_guide.jpg)

---

## 🚀 Quick Start

1. **Install the Unity package** (see above)
2. **Open your Unity project in Antigravity IDE**
3. **Install DotRush** when prompted (or manually)
4. In Unity Editor: **Edit → Preferences → External Tools → External Script Editor → Antigravity IDE**
5. Click **"Regenerate project files"** in the Antigravity IDE preferences panel
6. Done! IntelliSense, debugging, and syntax highlighting are ready.

<a href='https://ko-fi.com/Y8Y61ABMM' target='_blank'>
  <img width='400' src='https://storage.ko-fi.com/cdn/kofi2.png?v=6' alt='Support me on Ko-fi' />
</a>

---

## ⚡ Performance

### Before (standard approach)
- ~155 `.csproj` files for a typical Unity project
- Roslyn loads ALL assemblies including read-only packages
- Load time: **30-60 seconds**

### After (Antigravity optimized)
- ~10-15 `.csproj` files (user-editable only)
- Package types resolved via DLL HintPaths (no source parsing needed)
- Load time: **2-5 seconds** ⚡

---

## 🏗️ Architecture

```
UnityAntigravityIDE/
├── Editor/                          # Unity Editor scripts (the UPM package)
│   ├── AntigravityScriptEditor.cs   # IDE integration, preferences UI
│   ├── ProjectGeneration.cs         # .csproj/.sln generation engine
│   ├── UnityAnalyzerConfig.cs       # Roslyn analyzer configuration
│   └── UnityDebugBridge.cs          # Debug bridge for Unity
├── package.json                     # UPM package manifest
├── .githooks/                       # Local git hooks
│   └── pre-commit                   # Auto-increment patch version on commit
├── antigravity-unity-extension~/    # VS Code extension (local dev only)
│   └── release-extension.py         # Cross-platform release automator
├── DotRush~/                        # DotRush reference (local dev only)
├── com.unity.ide.vscode~/           # VS Code IDE reference (local dev only)
└── vscode-unity-debug~/             # Unity debugger reference (local dev only)
```

> **Note**: Folders ending with `~` are ignored by Unity Package Manager and are not tracked in git (dev-only references).

---

## 🔧 Configuration

### `.vscode/settings.json` (auto-generated)
```json
{
  "dotnet.defaultSolution": "YourProject.sln",
  "dotrush.roslyn.projectOrSolutionFiles": ["path/to/YourProject.sln"],
  "dotrush.msbuildProperties": {
    "DefineConstants": "UNITY_EDITOR"
  }
}
```

### Unity Preferences
- **External Script Editor**: Antigravity IDE
- **Editor arguments**: customizable in preferences
- **Generate .csproj**: automatic on script/asset changes

---

## 📝 Versioning

Version is auto-incremented via a local git pre-commit hook:
- Patch bumps on every commit (e.g., `2.1.7` → `2.1.8`)
- Uses `.githooks/pre-commit` (cross-platform bash, works on macOS and Linux)
- Set up: `git config core.hooksPath .githooks`

---

## ❓ FAQ / Troubleshooting

### IntelliSense not working — no autocomplete or suggestions

This is the most common issue. Check these in order:

| Check | Fix |
|-------|-----|
| DotRush not installed | Install `nromanov.dotrush` from Extensions or [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) |
| Antigravity Unity extension not installed | Install from [Marketplace](https://open-vsx.org/extension/antigravity-unity/antigravity-unity) or [GitHub Releases](https://github.com/billythekidz/UnityAntigravityIDE/releases/latest) |
| Wrong solution file selected | When DotRush prompts, pick the **`.sln`** file — not `.csproj` or `.slnx` |
| No `.sln` file exists | In Unity: **Edit → Preferences → External Tools → Regenerate project files** |
| DotRush not activated | Check Extensions panel — it must be **enabled**, not just installed |

After fixing, run `Ctrl+Shift+P` → `Developer: Reload Window`.

### Special characters in folder/project names (`&`, `+`, `#`, etc.)

If any folder in your Unity project path contains special characters like `&`, `+`, `#`, or non-ASCII characters, DotRush/Roslyn may fail to parse `.csproj` files. **Rename the folder** to use only alphanumeric characters, dashes, and underscores.

### "Why can't I just install Microsoft's C# extension?"

Microsoft's **C#**, **C# Dev Kit**, and **Unity** extensions are **licensed exclusively for Visual Studio Code**. They cannot be installed on Antigravity IDE, VSCodium, or any other VS Code fork. This is a Microsoft licensing policy, not a bug.

Our solution: **DotRush** — an open-source, MIT-licensed C# language server built on Roslyn that provides the same core IntelliSense features.

### "Antigravity (internal)" shows in Unity instead of the real editor

**Cause:** Your Unity project has **compile errors** in C# scripts. When scripts fail to compile, Unity cannot load the Antigravity package properly, so it falls back to showing "(internal)".

**Fix:**
1. Open Unity's **Console** window and look for red error messages.
2. Fix all C# compilation errors in your project.
3. Once scripts compile successfully, go to **Edit → Preferences → External Tools** — the dropdown should now show **"Antigravity"** (not "internal").

> **Tip:** Common culprits include `[SerializeField]` on auto-properties (not supported on some Unity versions), missing assembly references, or outdated third-party packages.

### Antigravity IDE is not listed in Unity's External Script Editor dropdown

| Cause | Fix |
|-------|-----|
| Antigravity IDE is not installed | Install from [Antigravity Downloads](https://antigravity.dev) or your package manager |
| App is not in `/Applications/` (macOS) | Move `Antigravity.app` to `/Applications/` |
| Unity package not installed | Add the git URL to `Packages/manifest.json` (see [Installation](#-installation)) |
| Project has compile errors | Fix all C# errors first (see above) |

### DotRush IntelliSense is not working

1. Confirm **DotRush** is installed: open Extensions in Antigravity IDE and search for `nromanov.dotrush`.
2. In Unity: **Edit → Preferences → External Tools → Regenerate project files**.
3. Make sure `.vscode/settings.json` contains `"dotnet.defaultSolution"` pointing to your `.sln`.
4. Restart Antigravity IDE after regenerating project files.

### Windows: `.NET SDK not found` or `dotnet` command errors

DotRush requires the .NET SDK to be installed and accessible:

1. Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download) (**SDK**, not just Runtime).
2. After installation, **restart Antigravity IDE** (not just reload window).
3. Verify in terminal: `dotnet --version` should return a version number.
4. If `dotnet` is still not found, add it to your PATH manually: `C:\Program Files\dotnet\`

### Windows: Long path errors (`MAX_PATH` exceeded)

Unity projects nested deep in folders can hit Windows' 260-character path limit:

1. Open **Registry Editor** → `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem`
2. Set `LongPathsEnabled` to `1`
3. Or run in an elevated PowerShell:
   ```powershell
   New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
   ```
4. Restart your machine.

### Windows: Firewall or proxy blocks extension downloads

Some corporate/school environments block Open VSX or GitHub:

- Download `.vsix` files manually from [Open VSX](https://open-vsx.org/extension/nromanov/dotrush) or [GitHub Releases](https://github.com/billythekidz/UnityAntigravityIDE/releases/latest).
- Install via `Ctrl+Shift+P` → `Extensions: Install from VSIX...`.

### Windows: `Unable to watch for file changes` error

Unity projects generate thousands of files in `Library/` and `Temp/`. Add these exclusions to your workspace settings:

```json
{
  "files.watcherExclude": {
    "**/Library/**": true,
    "**/Temp/**": true,
    "**/obj/**": true
  }
}
```

### Windows: Antivirus flags DotRush or the extension

Some antivirus software (especially Windows Defender) may quarantine DotRush's language server binary. Add an exclusion for your Antigravity IDE installation folder and the `.dotnet` tools directory.

### macOS: "open" command errors or editor doesn't launch

- Ensure `Antigravity.app` is in `/Applications/` (not `~/Downloads/`).
- If Gatekeeper blocks the app, right-click → **Open** to bypass.
- The Unity package handles macOS `.app` bundles automatically — you don't need to point to the inner `Contents/MacOS/` binary.

### Debugging: "Cannot attach to Unity" or no instances found

1. Make sure Unity Editor is **running** with your project open.
2. In Antigravity IDE: `Ctrl+Shift+P` → `Antigravity: Attach Unity Debugger`.
3. If no instances appear, check that Unity's **Script Debugging** is enabled in Build Settings.
4. On Windows, ensure your firewall isn't blocking the debugger port.

### How do I set up the git hooks after cloning?

Run this once after cloning the repo:
```bash
git config core.hooksPath .githooks
```
This enables the pre-commit hook that auto-bumps the Unity package version.

---

## 📄 License

MIT

