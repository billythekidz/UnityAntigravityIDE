#!/usr/bin/env python3
"""
Antigravity Unity Extension Release Automator (Cross-platform)
Python equivalent of release-extension.ps1

This script:
1. Bumps the patch version in extension package.json
2. Packages the extension into a .vsix using vsce
3. Publishes to Open VSX
4. Creates a GitHub Release and uploads the .vsix

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

    # 4. Package extension
    warn("Packaging Extension...")
    vsix_name = f"antigravity-unity-{new_version}.vsix"
    run(f"npx -y vsce package --no-git-tag-version -o {vsix_name}", cwd=EXTENSION_DIR)

    vsix_path = os.path.join(EXTENSION_DIR, vsix_name)
    if not os.path.isfile(vsix_path):
        err(f"VSIX file not found: {vsix_path}")
        sys.exit(1)

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
    run(
        f'gh release create "v{new_version}" "{relative_vsix}" '
        f'--title "{release_title}" '
        f'--notes "{release_notes}"',
        cwd=PROJECT_ROOT
    )
    ok("GitHub Release created!")

    info(f"RELEASE COMPLETE: Antigravity Unity v{new_version} is now LIVE!")


if __name__ == "__main__":
    main()
