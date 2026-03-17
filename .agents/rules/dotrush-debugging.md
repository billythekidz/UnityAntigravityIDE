---
description: How to find and read DotRush extension logs for debugging compilation issues
---

# DotRush Extension Log Locations

## DotRush Language Server Logs (Primary)

DotRush writes its own logs directly to:

```
~/.antigravity/extensions/nromanov.dotrush-<version>-<platform>/extension/bin/LanguageServer/logs/
```

Files:
- **`Error.log`** — Critical errors (MSBuild not found, dotnet path issues, workspace load failures)
- **`Debug.log`** — Verbose debug output (project loading, compilation steps)

### Quick access:
```bash
# Error log (most useful for debugging)
cat ~/.antigravity/extensions/nromanov.dotrush-*/extension/bin/LanguageServer/logs/Error.log

# Debug log
cat ~/.antigravity/extensions/nromanov.dotrush-*/extension/bin/LanguageServer/logs/Debug.log

# DevHost logs (less common)
cat ~/.antigravity/extensions/nromanov.dotrush-*/extension/bin/DevHost/logs/Debug.log
```

## Antigravity IDE Extension Host Logs (Secondary)

Shows when DotRush extension activates/deactivates:

```
~/Library/Application Support/Antigravity/logs/<session>/window<N>/exthost/exthost.log
```

### Quick access:
```bash
# Find latest session
ls -t ~/Library/Application\ Support/Antigravity/logs/ | head -1

# Search for DotRush activation in latest window's exthost log
grep -i "dotrush" ~/Library/Application\ Support/Antigravity/logs/$(ls -t ~/Library/Application\ Support/Antigravity/logs/ | head -1)/window*/exthost/exthost.log
```

## DotRush Process Check

```bash
# Check if DotRush language server is running
ps aux | grep -i "DotRush" | grep -v grep
```

## Common Errors and Fixes

### `Path to dotnet executable is not set`
**Cause:** GUI apps on macOS don't inherit shell PATH.
**Fix:** Add `dotrush.roslyn.dotnetSdkDirectory` to `.vscode/settings.json`:
```json
{
    "dotrush.roslyn.dotnetSdkDirectory": "/usr/local/share/dotnet/sdk/9.0.306"
}
```
The SDK directory can be found with: `dotnet --list-sdks`

### `MSB3644: Reference assemblies not found`
**Cause:** .NET Framework v4.7.1 reference assemblies missing (no Mono on macOS/Linux).
**Fix:** `FrameworkPathOverride` in `Directory.Build.props` pointing to Unity's `unity-4.8-api/`.

### `CS2001: Source file not found` (for Packages/ paths)
**Cause:** Unity virtual paths like `Packages/com.foo/Editor/Bar.cs` don't exist on disk.
**Fix:** Resolve via `PackageInfo.FindForAssetPath()` in `ProjectGeneration.cs`.
