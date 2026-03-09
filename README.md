# Antigravity IDE Support for Unity

*Note: This repository is a fork of [prithvi-bharadwaj/Antigravity-IDE](https://github.com/prithvi-bharadwaj/Antigravity-IDE).*

Full-featured integration for **Antigravity IDE** as an external script editor for Unity, providing IntelliSense, debugging, Roslyn analyzers, and seamless project file generation.

## Features

### Core
- **Auto-Discovery**: Automatically detects Antigravity installation on macOS, Windows, and Linux.
- **Smart Opening**: Opens files at specific lines and columns with workspace-first arguments.
- **Reuse Window**: Opens files in existing Antigravity window via `--reuse-window` flag.

### IntelliSense & Project Generation
- Generates `.csproj` and `.sln` files with proper `LangVersion` (C# 8/9/10 based on Unity version).
- Includes `DefineConstants` from Unity scripting defines and platform symbols.
- Discovers and includes Roslyn analyzer references from Unity packages.
- Generates `Directory.Build.props` for analyzer configuration.
- Smart sync — only regenerates when `.cs` or `.asmdef` files change.

### Debugging
- **Debug Bridge**: TCP-based debug server inside Unity Editor (menu: `Antigravity > Start Debug Bridge`).
- Auto-generates `.vscode/launch.json` with Unity debugger configurations.
- Supports attaching to Unity Editor and standalone Players.
- Remote commands: play, pause, stop, and debug info queries.

### Analyzer Configuration
- Generates `.editorconfig` with Unity-appropriate diagnostic suppressions.
- Suppresses false positives for Unity API messages (`Start`, `Update`, `OnCollisionEnter`, etc.).
- Configurable analyzer levels: None, Default, Recommended, All.
- Menu: `Antigravity > Generate Analyzer Config`.

### Preferences GUI
Access via **Unity > Preferences > External Tools** (select Antigravity):
- **Reuse Window** — toggle reuse vs new window behavior
- **Debug Port** — configure TCP port for debug bridge
- **Generate launch.json** — auto-generate debug configs
- **Analyzer Level** — set Roslyn diagnostic severity
- **Regenerate / Reset** buttons

## Installation

### via Package Manager (Git URL)
1. Open Unity (2021.3 or later).
2. Go to **Window > Package Manager**.
3. Click **+** > **Add package from git URL...**.
4. Enter: `https://github.com/billythekidz/UnityAntigravityIDE.git`

### via Disk
1. Open **Window > Package Manager**.
2. Click **+** > **Add package from disk...**.
3. Select the `package.json` file in this folder.

## Usage

1. Go to **Unity > Preferences > External Tools**.
2. Select **Antigravity** as External Script Editor.
3. (Optional) Start the debug bridge via **Antigravity > Start Debug Bridge**.
4. (Optional) Generate analyzer config via **Antigravity > Generate Analyzer Config**.

## Submodules (Reference)

| Submodule | Purpose |
|---|---|
| `vscode-csharp` | C# extension source — reference for language server integration |
| `vscode-dotnettools` | C# Dev Kit issue tracker — reference for feature parity |

## Requirements
- Unity 2021.3 LTS or later
- Antigravity IDE installed

## Troubleshooting
- **Editor not found**: The package checks common installation paths. If installed elsewhere, browse and select the executable manually.
- **No IntelliSense**: Click **Regenerate Project Files** in Preferences, or ensure `.csproj` files are generated.
- **Debug bridge fails**: Check if the configured port is available (default: 56000).
