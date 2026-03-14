---
activation: Model Decision
description: Ensures that extension releases use the centralized release-extension.py script.
---

# Extension Release Management

Whenever you need to publish or release a new version of the **Antigravity Unity** extension to Open VSX or GitHub, you MUST use the centralized release script.

## 📜 Release Script
Target: `antigravity-unity-extension~/release-extension.py` (cross-platform, run from the repo root).

## 📝 Commit Message Convention
The script accepts an optional `-m` / `--message` flag for a descriptive release summary following **Conventional Commits**:

```bash
python3 antigravity-unity-extension~/release-extension.py -m "fix: add macOS Unity editor support"
```

This produces a commit like:
```
release(v1.2.16): fix: add macOS Unity editor support [skip ci]
```

### When writing the `-m` message:
- Use a Conventional Commits prefix: `feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `perf:`, `style:`, `test:`
- Keep it concise (under 72 characters after the prefix)
- Describe **what changed**, not implementation details
- Examples:
  - `fix: resolve macOS .app bundle detection in Unity editor`
  - `feat: add ShaderLab syntax highlighting`
  - `chore: update dependencies and clean up build`

If `-m` is omitted, the commit defaults to `release(vX.Y.Z): patch release [skip ci]`.

## 🚀 Behavior
1. **Never** manually run `ovsx publish` or `vsce package` if the goal is a formal release.
2. **Never** manually create GitHub tags or releases for the extension.
3. Instead, run `python3 antigravity-unity-extension~/release-extension.py` from the repo root.
4. **Always** provide a `-m` message describing the changes in the release.
5. The script automatically:
   - Bumps the patch version in `antigravity-unity-extension~/package.json`.
   - Packages the `.vsix`.
   - Publishes to Open VSX.
   - Commits with the formatted message, tags, and pushes.
   - Creates a GitHub Release with the `.vsix` attached.

## 🛠️ Verification
Before running the script, ensure:
- The `ovsx-token.txt` is present in `.secrets/`.
- GitHub CLI (`gh`) is authenticated.
- You have reviewed the latest changes in the README.
