#!/usr/bin/env python3
"""
Build a .unitypackage from the Antigravity IDE Unity package.

A .unitypackage is a gzipped tar archive where each asset is stored as:
  <guid>/
    asset      - the file content
    asset.meta - the .meta file content
    pathname   - text file with the target path (e.g. Assets/Plugins/AntigravityIDE/Editor/File.cs)

Usage:
    python3 build-unitypackage.py [--output <path>]
"""

import os
import re
import sys
import tarfile
import tempfile
import json

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)

# Target install path inside the user's Unity project
INSTALL_PREFIX = "Assets/Plugins/AntigravityIDE"

# Files/folders to include (relative to PROJECT_ROOT)
INCLUDE_FILES = [
    "Editor/AntigravityScriptEditor.cs",
    "Editor/ProjectGeneration.cs",
    "Editor/UnityAnalyzerConfig.cs",
    "Editor/UnityDebugBridge.cs",
    "Editor/Antigravity.Ide.Editor.asmdef",
    "package.json",
    "README.md",
]

# Folders to include (we also need .meta for the folder itself)
INCLUDE_FOLDERS = [
    "Editor",
]


def extract_guid(meta_path):
    """Extract the GUID from a .meta file."""
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"guid:\s*([a-f0-9]+)", line)
            if m:
                return m.group(1)
    raise ValueError(f"No GUID found in {meta_path}")


def add_asset_to_tar(tar, guid, pathname, asset_path, meta_path, tmpdir):
    """Add a single asset entry to the tar archive."""
    guid_dir = os.path.join(tmpdir, guid)
    os.makedirs(guid_dir, exist_ok=True)

    # pathname file
    pathname_file = os.path.join(guid_dir, "pathname")
    with open(pathname_file, "w", encoding="utf-8") as f:
        f.write(pathname)
    tar.add(pathname_file, arcname=f"{guid}/pathname")

    # asset.meta file
    meta_dest = os.path.join(guid_dir, "asset.meta")
    with open(meta_path, "r", encoding="utf-8") as src:
        with open(meta_dest, "w", encoding="utf-8") as dst:
            dst.write(src.read())
    tar.add(meta_dest, arcname=f"{guid}/asset.meta")

    # asset file (skip for folders)
    if asset_path and os.path.isfile(asset_path):
        asset_dest = os.path.join(guid_dir, "asset")
        with open(asset_path, "rb") as src:
            with open(asset_dest, "wb") as dst:
                dst.write(src.read())
        tar.add(asset_dest, arcname=f"{guid}/asset")


def main():
    # Parse args
    output = None
    args = sys.argv[1:]
    for i, arg in enumerate(args):
        if arg == "--output" and i + 1 < len(args):
            output = args[i + 1]

    # Default output name from package.json version
    if output is None:
        pkg_path = os.path.join(PROJECT_ROOT, "package.json")
        with open(pkg_path, "r", encoding="utf-8") as f:
            pkg = json.load(f)
        version = pkg.get("version", "0.0.0")
        output = os.path.join(PROJECT_ROOT, f"AntigravityIDE-{version}.unitypackage")

    print(f"Building .unitypackage...")
    print(f"  Source:  {PROJECT_ROOT}")
    print(f"  Output:  {output}")
    print(f"  Install: {INSTALL_PREFIX}/")

    with tempfile.TemporaryDirectory() as tmpdir:
        with tarfile.open(output, "w:gz") as tar:
            asset_count = 0

            # Add folders first (they need their own GUID entry)
            for folder in INCLUDE_FOLDERS:
                meta_path = os.path.join(PROJECT_ROOT, f"{folder}.meta")
                if not os.path.exists(meta_path):
                    print(f"  WARNING: {folder}.meta not found, skipping folder entry")
                    continue

                guid = extract_guid(meta_path)
                pathname = f"{INSTALL_PREFIX}/{folder}"
                add_asset_to_tar(tar, guid, pathname, None, meta_path, tmpdir)
                asset_count += 1
                print(f"  + {pathname} (folder)")

            # Add files
            for filepath in INCLUDE_FILES:
                asset_path = os.path.join(PROJECT_ROOT, filepath)
                meta_path = os.path.join(PROJECT_ROOT, f"{filepath}.meta")

                if not os.path.exists(asset_path):
                    print(f"  WARNING: {filepath} not found, skipping")
                    continue
                if not os.path.exists(meta_path):
                    print(f"  WARNING: {filepath}.meta not found, skipping")
                    continue

                guid = extract_guid(meta_path)
                pathname = f"{INSTALL_PREFIX}/{filepath}"
                add_asset_to_tar(tar, guid, pathname, asset_path, meta_path, tmpdir)
                asset_count += 1
                print(f"  + {pathname}")

    size_kb = os.path.getsize(output) / 1024
    print(f"\nDone! {asset_count} assets, {size_kb:.1f} KB")
    print(f"Output: {output}")


if __name__ == "__main__":
    main()
