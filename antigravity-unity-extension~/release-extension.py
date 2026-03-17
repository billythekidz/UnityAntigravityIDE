#!/usr/bin/env python3
"""
Antigravity Unity Extension Release Automator (Cross-platform)
Python equivalent of release-extension.ps1

This script:
1. Bumps the patch version in extension package.json
2. Packages the extension into a .vsix using vsce
3. Builds a .unitypackage for offline installation
4. Publishes to Open VSX
5. Creates a GitHub Release and uploads both .vsix and .unitypackage

Usage:
  python3 antigravity-unity-extension~/release-extension.py
  python3 antigravity-unity-extension~/release-extension.py -m "fix: add macOS editor support"
  python3 antigravity-unity-extension~/release-extension.py --message "feat: new debugging panel"

  (run from the repo root: UnityAntigravityIDE/)

The optional -m/--message flag adds a descriptive summary to the commit and
GitHub release following Conventional Commits convention:
  release(v1.2.15): fix: add macOS editor support [skip ci]
Without -m, the commit message defaults to:
  release(v1.2.15): patch release [skip ci]
"""

import argparse
import json
import os
import subprocess
import sys
import re
import tarfile
import tempfile

# ─── Resolve paths ───────────────────────────────────────────────────
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
EXTENSION_DIR = SCRIPT_DIR

PACKAGE_JSON_PATH = os.path.join(EXTENSION_DIR, "package.json")
UNITY_PKG_JSON_PATH = os.path.join(PROJECT_ROOT, "package.json")
SECRETS_FILE = os.path.join(PROJECT_ROOT, ".secrets", "ovsx-token.txt")

# ─── Helpers ─────────────────────────────────────────────────────────
CYAN = "\033[0;36m"
GREEN = "\033[0;32m"
YELLOW = "\033[1;33m"
RED = "\033[0;31m"
NC = "\033[0m"

def info(msg):  print(f"{CYAN}{msg}{NC}")
def ok(msg):    print(f"{GREEN}{msg}{NC}")
def warn(msg):  print(f"{YELLOW}{msg}{NC}")
def err(msg):   print(f"{RED}{msg}{NC}", file=sys.stderr)

def run(cmd, cwd=None, check=True):
    """Run a shell command, streaming output."""
    print(f"  $ {cmd}")
    result = subprocess.run(cmd, shell=True, cwd=cwd, check=check)
    return result

# ─── Unity Package Builder ───────────────────────────────────────────
INSTALL_PREFIX = "Assets/Plugins/AntigravityIDE"
INCLUDE_FOLDERS = ["Editor"]
INCLUDE_FILES = [
    "Editor/AntigravityScriptEditor.cs",
    "Editor/ProjectGeneration.cs",
    "Editor/UnityAnalyzerConfig.cs",
    "Editor/UnityDebugBridge.cs",
    "Editor/Antigravity.Ide.Editor.asmdef",
    "package.json",
    "README.md",
]

def extract_guid(meta_path):
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"guid:\s*([a-f0-9]+)", line)
            if m:
                return m.group(1)
    raise ValueError(f"No GUID found in {meta_path}")

def build_unitypackage(project_root, output_path):
    """Build a .unitypackage from the project files."""
    with tempfile.TemporaryDirectory() as tmpdir:
        with tarfile.open(output_path, "w:gz") as tar:
            entries = []
            # Folders
            for folder in INCLUDE_FOLDERS:
                meta = os.path.join(project_root, f"{folder}.meta")
                if os.path.exists(meta):
                    guid = extract_guid(meta)
                    pathname = f"{INSTALL_PREFIX}/{folder}"
                    _add_tar_entry(tar, tmpdir, guid, pathname, None, meta)
                    entries.append(pathname)
            # Files
            for filepath in INCLUDE_FILES:
                asset = os.path.join(project_root, filepath)
                meta = os.path.join(project_root, f"{filepath}.meta")
                if os.path.exists(asset) and os.path.exists(meta):
                    guid = extract_guid(meta)
                    pathname = f"{INSTALL_PREFIX}/{filepath}"
                    _add_tar_entry(tar, tmpdir, guid, pathname, asset, meta)
                    entries.append(pathname)
    print(f"  {len(entries)} assets packaged")

def _add_tar_entry(tar, tmpdir, guid, pathname, asset_path, meta_path):
    gdir = os.path.join(tmpdir, guid)
    os.makedirs(gdir, exist_ok=True)
    # pathname
    pf = os.path.join(gdir, "pathname")
    with open(pf, "w") as f: f.write(pathname)
    tar.add(pf, arcname=f"{guid}/pathname")
    # asset.meta
    mf = os.path.join(gdir, "asset.meta")
    with open(meta_path, "r") as s, open(mf, "w") as d: d.write(s.read())
    tar.add(mf, arcname=f"{guid}/asset.meta")
    # asset (skip for folders)
    if asset_path and os.path.isfile(asset_path):
        af = os.path.join(gdir, "asset")
        with open(asset_path, "rb") as s, open(af, "wb") as d: d.write(s.read())
        tar.add(af, arcname=f"{guid}/asset")

# ─── Main ────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="Antigravity Unity Extension Release Automator"
    )
    parser.add_argument(
        "-m", "--message",
        type=str,
        default=None,
        help=(
            "Release description following Conventional Commits convention. "
            "Examples: 'fix: add macOS editor support', "
            "'feat: shader syntax highlighting', "
            "'chore: update dependencies'"
        )
    )
    args = parser.parse_args()

    info("--- Starting Release Process ---")
    print(f"Project Root:  {PROJECT_ROOT}")
    print(f"Extension Dir: {EXTENSION_DIR}")

    # 1. Validate environment
    if not os.path.isfile(SECRETS_FILE):
        err(f"Missing Open VSX token at {SECRETS_FILE}. Please ensure .secrets/ovsx-token.txt exists.")
        sys.exit(1)

    with open(SECRETS_FILE, "r") as f:
        ovsx_token = f.read().strip()

    if not ovsx_token:
        err("Open VSX token is empty.")
        sys.exit(1)

    # Check for gh CLI
    try:
        subprocess.run("gh --version", shell=True, capture_output=True, check=True)
    except (subprocess.CalledProcessError, FileNotFoundError):
        warn("Warning: GitHub CLI (gh) is not installed. GitHub Release step will fail.")

    # 2. Bump Version (Patch)
    warn("Bumping version...")
    with open(PACKAGE_JSON_PATH, "r", encoding="utf-8") as f:
        pkg = json.load(f)

    current_version = pkg["version"]
    parts = current_version.split(".")
    parts[2] = str(int(parts[2]) + 1)
    new_version = ".".join(parts)
    pkg["version"] = new_version

    with open(PACKAGE_JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(pkg, f, indent=4, ensure_ascii=False)
        f.write("\n")

    ok(f"Extension version bumped to v{new_version}")

    # Also bump root Unity package.json (since pre-commit hook is skipped via --no-verify)
    with open(UNITY_PKG_JSON_PATH, "r", encoding="utf-8") as f:
        unity_pkg = json.load(f)

    unity_current = unity_pkg["version"]
    unity_parts = unity_current.split(".")
    unity_parts[2] = str(int(unity_parts[2]) + 1)
    unity_new = ".".join(unity_parts)
    unity_pkg["version"] = unity_new

    with open(UNITY_PKG_JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(unity_pkg, f, indent=2, ensure_ascii=False)
        f.write("\n")

    ok(f"Unity package version bumped to v{unity_new}")

    # 3. Build commit message & release notes
    description = args.message if args.message else "patch release"
    commit_msg = f"release(v{new_version}): {description} [skip ci]"
    release_title = f"Antigravity Unity v{new_version}"
    release_notes = description if args.message else f"Automated release of Antigravity Unity extension version {new_version}."

    info(f"Commit: {commit_msg}")
    info(f"Release title: {release_title}")

    # 3b. Update CHANGELOG.md
    changelog_path = os.path.join(EXTENSION_DIR, "CHANGELOG.md")
    warn("Updating CHANGELOG.md...")
    new_entry = f"\n## v{new_version}\n- {description}\n"
    if os.path.isfile(changelog_path):
        with open(changelog_path, "r", encoding="utf-8") as f:
            content = f.read()
        # Insert new entry before the first version heading (## v...)
        first_version = content.find("\n## v")
        if first_version >= 0:
            content = content[:first_version] + new_entry + content[first_version:]
        else:
            content = content.rstrip() + "\n" + new_entry
    else:
        content = "# Changelog\n\nAll notable changes to the Antigravity Unity extension will be documented in this file.\n" + new_entry

    with open(changelog_path, "w", encoding="utf-8") as f:
        f.write(content)
    ok(f"CHANGELOG.md updated with v{new_version}")

    # 4. Package extension
    warn("Packaging Extension...")
    vsix_name = f"antigravity-unity-{new_version}.vsix"
    run(f"npx -y vsce package --no-git-tag-version -o {vsix_name}", cwd=EXTENSION_DIR)

    vsix_path = os.path.join(EXTENSION_DIR, vsix_name)
    if not os.path.isfile(vsix_path):
        err(f"VSIX file not found: {vsix_path}")
        sys.exit(1)

    # 4b. Build .unitypackage
    warn("Building .unitypackage...")
    unitypackage_name = f"AntigravityIDE-{unity_new}.unitypackage"
    unitypackage_path = os.path.join(EXTENSION_DIR, unitypackage_name)
    build_unitypackage(PROJECT_ROOT, unitypackage_path)
    ok(f"Built {unitypackage_name}")

    # 5. Publish to Open VSX
    warn("Publishing to Open VSX...")
    run(f"npx -y ovsx publish {vsix_name} --pat {ovsx_token}", cwd=EXTENSION_DIR)
    ok("Published to Open VSX successfully!")

    # 6. Git Commit & Push
    warn("Committing changes to Git...")
    run("git add .", cwd=PROJECT_ROOT)
    run(f'git commit --no-verify -m "{commit_msg}"', cwd=PROJECT_ROOT)
    # Use --no-verify to bypass githooks that might rewrite history (amend)
    run("git push --no-verify", cwd=PROJECT_ROOT)
    run(f"git tag v{new_version}", cwd=PROJECT_ROOT)
    run(f"git push origin v{new_version} --no-verify", cwd=PROJECT_ROOT)

    # 7. GitHub Release
    warn("Creating GitHub Release...")
    relative_vsix = f"antigravity-unity-extension~/antigravity-unity-{new_version}.vsix"
    relative_unity = f"antigravity-unity-extension~/{unitypackage_name}"
    run(
        f'gh release create "v{new_version}" "{relative_vsix}" "{relative_unity}" '
        f'--title "{release_title}" '
        f'--notes "{release_notes}"',
        cwd=PROJECT_ROOT
    )
    ok("GitHub Release created!")

    info(f"RELEASE COMPLETE: Antigravity Unity v{new_version} is now LIVE!")


if __name__ == "__main__":
    main()
