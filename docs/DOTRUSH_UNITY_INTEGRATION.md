# DotRush ↔ Unity Integration Guide

> How DotRush (JaneySprings/DotRush) provides C# IntelliSense and debugging for Unity projects,
> and what Antigravity IDE fills in to complete the experience.

---

## Architecture Overview

```
┌─────────────────────────────┐     ┌──────────────────────────────┐
│      Unity Editor           │     │       VS Code                │
│                             │     │                              │
│  AssetPostprocessor         │     │  Antigravity Unity Extension │
│    ↓                        │     │    ↓                         │
│  ProjectGeneration.cs       │     │  FileSystemWatcher           │
│    ↓                        │     │  (*.csproj, *.sln)           │
│  Writes .csproj/.sln files  │────→│    ↓                         │
│                             │     │  dotrush.reloadWorkspace     │
│                             │     │    ↓                         │
│                             │     │  DotRush Language Server     │
│                             │     │    ↓                         │
│                             │     │  MSBuildWorkspace → Roslyn   │
│                             │     │    ↓                         │
│                             │     │  IntelliSense + Diagnostics  │
└─────────────────────────────┘     └──────────────────────────────┘
```

## DotRush Native Unity Features

| Feature | Status | Source Files |
|---------|--------|-------------|
| **Unity Debugger** (Mono) | ✅ Built-in | `src/VSCode/providers/monoDebugConfigurationProvider.ts` |
| **Debug type `"unity"`** | ✅ Registered | `package.json` → debuggers section |
| **Android attach** | ✅ Supported | `transportArgs: { type: "android" }` |
| **il2cpp handling** | ✅ Auto-disabled | `allowToStringCalls: false` when `transportArgs` present |
| **C# IntelliSense** | ✅ Via Roslyn | `src/DotRush.Roslyn.Server/` |
| **`.csproj` file watching** | ⚠️ Editor-only | `onDidSaveTextDocument` — misses external writes from Unity |
| **Unity project generation** | ❌ Not included | Requires external tool (Antigravity IDE) |
| **Unity scripting defines** | ❌ Not auto-set | Must configure `dotrush.msbuildProperties` |
| **NuGet restore skip** | ❌ Default: enabled | `restoreProjectsBeforeLoading: true` fails on Unity `.csproj` |

## What Antigravity IDE Provides

### 1. Project Generation (`Editor/ProjectGeneration.cs`)
- Generates `.csproj` and `.sln` files from Unity's `CompilationPipeline`
- Includes **all assemblies**: Player (with tests) + Editor
- Handles Unity 6 modular DLLs via `asmdefCompileOutputPath`
- Parses `.rsp` response files for additional defines
- Adds `<HintPath>` references for all Unity DLLs

### 2. VS Code Settings (`Editor/ProjectGeneration.cs` → `WriteVSCodeSettingsFiles`)
Generates `.vscode/settings.json` with:
```json
{
    "dotnet.defaultSolution": "project-name.sln",
    "dotrush.msbuildProperties": {
        "DefineConstants": "UNITY_EDITOR"
    },
    "dotrush.roslyn.projectOrSolutionFiles": ["path/to/project.sln"],
    "dotrush.roslyn.restoreProjectsBeforeLoading": false
}
```

### 3. External `.csproj` Change Detection (`antigravity-unity-extension~/src/extension.ts`)
DotRush's built-in watcher (`onDidSaveTextDocument`) only detects saves **from within VS Code**.
When Unity regenerates `.csproj` files externally, DotRush misses the change.

The Antigravity extension fills this gap:
- Creates `FileSystemWatcher` for `*.csproj` and `*.sln` files
- Debounces 2s (Unity may regenerate multiple projects in sequence)
- Calls `dotrush.reloadWorkspace` to trigger full MSBuild re-load

### 4. Unity Debugger Attachment (`antigravity-unity-extension~/src/commands/commands.ts`)
- Registers `Attach Unity Debugger` command
- Uses DotRush's `"unity"` debug type

## DotRush Key Source Files (Reference)

> Source: [github.com/JaneySprings/DotRush](https://github.com/JaneySprings/DotRush)

### Language Server (C#)
| File | Purpose |
|------|---------|
| `src/DotRush.Roslyn.Server/Services/WorkspaceService.cs` | MSBuild workspace loading, FileSystemWatcher for `.cs` files |
| `src/DotRush.Roslyn.Server/Services/CodeAnalysisService.cs` | Diagnostics analysis + publishing to editor |
| `src/DotRush.Roslyn.Server/Services/ConfigurationService.cs` | Settings: `CompilerDiagnosticsScope`, `RestoreProjects`, etc. |
| `src/DotRush.Roslyn.Server/Handlers/Framework/ReloadWorkspaceHandler.cs` | Handles `dotrush/reloadWorkspace` notification |
| `src/DotRush.Roslyn.Workspaces/DotRushWorkspace.cs` | Base workspace class — `InitializeWorkspace()`, `LoadAsync()` |
| `src/DotRush.Roslyn.Workspaces/FileSystem/WorkspaceFilesWatcher.cs` | Watches `.cs` file changes — Create/Delete/Rename |
| `src/DotRush.Roslyn.CodeAnalysis/CompilationHost.cs` | Roslyn compilation + diagnostic collection |
| `src/DotRush.Common/MSBuild/DefaultItemsRewriter.cs` | Modifies `.csproj` `<Compile>` entries (when `ApplyWorkspaceChanges=true`) |

### VS Code Extension (TypeScript)
| File | Purpose |
|------|---------|
| `src/VSCode/controllers/languageServerController.ts` | Client-side: `dotrush.reloadWorkspace` command, project change prompt |
| `src/VSCode/providers/monoDebugConfigurationProvider.ts` | Unity debugger configuration |
| `src/VSCode/extensions.ts` | Project/solution file discovery |
| `src/VSCode/resources/constants.ts` | `debuggerUnityId = "unity"` |

## DotRush Settings for Unity (Reference)

| Setting | Default | Recommended for Unity | Why |
|---------|---------|----------------------|-----|
| `dotrush.roslyn.restoreProjectsBeforeLoading` | `true` | **`false`** | `dotnet restore` fails on Unity `.csproj` |
| `dotrush.roslyn.compilerDiagnosticsScope` | `"Project"` | `"Project"` | Shows errors for entire project when any file is opened |
| `dotrush.roslyn.projectOrSolutionFiles` | `[]` | Set by Antigravity | Points to the generated `.sln` file |
| `dotrush.msbuildProperties` | `{}` | `{"DefineConstants": "UNITY_EDITOR"}` | Unity scripting defines |
| `dotrush.roslyn.skipUnrecognizedProjects` | `true` | `true` | Prevents crashes on non-standard Unity projects |

## Diagnostic Flow

```
1. User opens .cs file in VS Code
2. DotRush receives textDocument/didOpen
3. CodeAnalysisService.RequestDiagnosticsPublishing() triggered
4. CompilationHost.AnalyzeAsync() runs:
   - scope="Document" → GetSemanticModel → GetDiagnostics (single file)
   - scope="Project"  → GetCompilation → GetDiagnostics (all files in project)
   - scope="Solution"  → GetCompilation for ALL projects
5. Results stored in DiagnosticCollection (BeginUpdate/EndUpdate pattern)
6. PublishDiagnostics sent to editor → squiggles appear

Note: Diagnostics are ON-DEMAND — files that are never opened are never analyzed.
```

## Common Issues

### CS0246 errors not shown in DotRush
**Cause**: `restoreProjectsBeforeLoading: true` (default) runs `dotnet restore` which fails on Unity `.csproj`, potentially preventing proper project loading.  
**Fix**: Set `"dotrush.roslyn.restoreProjectsBeforeLoading": false` in `.vscode/settings.json` (auto-set by Antigravity IDE).

### Stale errors after deleting scripts
**Cause**: DotRush's `WorkspaceFilesWatcher` handles `.cs` deletions automatically (removes from Roslyn in-memory). But if Unity also regenerates `.csproj` (removing the `<Compile>` entry), DotRush needs a workspace reload.  
**Fix**: The Antigravity extension watches `.csproj` changes and calls `dotrush.reloadWorkspace`.

### IntelliSense missing for Unity types
**Cause**: The `.csproj` file doesn't include `<Reference>` entries for Unity DLLs, or `UNITY_EDITOR` define is missing.  
**Fix**: Run `Sync Project Files` from Unity Editor (Antigravity IDE regenerates everything).
