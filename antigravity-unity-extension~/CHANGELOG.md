# Changelog

All notable changes to the Antigravity Unity extension will be documented in this file.

## v1.2.46
- fix: preserve user files.exclude on settings.json regeneration

## v1.2.45
- fix: resolve UnityEditor types on all platforms (remove macOS-only guards)

## v1.2.44
- perf: fix slow play mode entry - skip unnecessary Sync on domain reload, cache dotnet detection

## v1.2.43
- fix: add changelog to vsix, optimize icon size, revert watcherExclude

## v1.2.42
- perf: fix slow play mode entry - set autoReferenced=false, defer Sync to delayCall, inject dotnet PATH for DotRush

## v1.2.41
- fix: detect dotnet via shell (which/where) before hardcoded fallbacks

## v1.2.40
- fix: set DOTNET_ROOT and DOTNET_HOST_PATH env vars for cross-platform DotRush dotnet discovery

## v1.2.39
- fix: auto-inject FrameworkPathOverride and resolve Packages/ virtual paths for cross-platform DotRush compilation

## v1.2.38
- fix: disable csproj fixer fallback (interferes with DotRush)

## v1.2.37
- fix: add fallback csproj fixer for missing assembly references on macOS

## v1.2.36
- fix: resize Ko-fi buttons in READMEs

## v1.2.35
- fix: add Ko-fi button to top of READMEs

## v1.2.34
- fix: reposition Ko-fi donation button in READMEs

## v1.2.33
- feat: add Ko-fi donation button to READMEs and sponsor field to package.json

## v1.2.31
- patch release

## v1.2.30
- patch release

## v1.2.29
- fix: macOS file opening via URL scheme, .unitypackage in releases, badges

## v1.2.28
- docs: add Open VSX badges to READMEs

## v1.2.27
- feat: include .unitypackage in GitHub releases for offline installation

## v1.2.26
- fix: use antigravity:// URL scheme for macOS file opening — no duplicate dock icons

## v1.2.25
- fix: restructure macOS open command for proper file and goto arg passing

## v1.2.22
- docs: add FAQ/troubleshooting section and update architecture in README

## v1.2.21
- chore: remove debug logging from editor detection

## v1.2.20
- fix: include README.md in package (was excluded by .npmignore wildcard)

## v1.2.19
- fix: handle Electron binary inside Antigravity.app on macOS

## v1.2.18
- fix: release script now bumps both extension and Unity package versions

## v1.2.17
- fix: macOS editor support, cross-platform release tooling, and pre-commit hook
