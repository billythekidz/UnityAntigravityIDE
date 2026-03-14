# Antigravity Unity IDE Support

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
- `Antigravity: Attach Unity Debugger` — attach to running Unity instance
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

### "Antigravity (internal)" shows in Unity instead of the real editor

**Cause:** Your Unity project has **compile errors** in C# scripts. When scripts fail to compile, Unity cannot load the Antigravity package properly, so it falls back to showing "(internal)".

**Fix:**
1. Open Unity's **Console** window and look for red error messages.
2. Fix all C# compilation errors in your project.
3. Once scripts compile successfully, go to **Edit → Preferences → External Tools** — the dropdown should now show **"Antigravity"** (not "internal").

> **Tip:** Common culprits include `[SerializeField]` on auto-properties (not supported on some Unity versions), missing assembly references, or outdated third-party packages.

### Antigravity IDE is not listed in Unity's External Script Editor dropdown

**Possible causes & fixes:**

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

### macOS: "open" command errors or editor doesn't launch

- Ensure `Antigravity.app` is in `/Applications/` (not `~/Downloads/`).
- If Gatekeeper blocks the app, right-click → **Open** to bypass.
- The Unity package handles macOS `.app` bundles automatically — you don't need to point to the inner `Contents/MacOS/` binary.

### How do I set up the git hooks after cloning?

Run this once after cloning the repo:
```bash
git config core.hooksPath .githooks
```
This enables the pre-commit hook that auto-bumps the Unity package version.

---

## 📄 License

MIT

