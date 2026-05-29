---
name: bump-version
description: >-
  Bump the project version in Directory.Build.props using semver.
  Trigger keywords: bump, version, major, minor, patch, release.
  Use ONLY when the user explicitly asks to bump the project version.
---

# Bump Version

## Steps

1. **Fetch tags**
   ```powershell
   git fetch --tags
   ```
   This ensures pipeline-pushed tags are available locally.

2. **Read current version**
   Read `Directory.Build.props` and extract `<Version>` between the tags.
   Parse as three integers: `major.minor.patch`.

3. **Bump the requested component**
   - `major` → increment major, set minor=0, patch=0
   - `minor` → increment minor, set patch=0
   - `patch` → increment patch
   If the user says e.g. "bump to 0.10.0", use that value directly.

4. **Gather commits since last tag**
   ```powershell
   $tag = git describe --tags --abbrev=0 2>$null
   if (-not $tag) { $tag = (git log --oneline | Select-Object -Last 1).Split(' ')[0] }
   git log "$tag..HEAD" --oneline --no-decorate
   ```
   Collect the subject lines (everything after the commit hash) for the release notes.

5. **Read current release notes**
   Read the `<PackageReleaseNotes>` block from `Directory.Build.props`.
   It contains version entries in this format:
   ```
   X.Y.Z
   - Description one.
   - Description two.
   X.Y.Z-1
   ...
   ```

6. **Update `Directory.Build.props`**
   - Change `<Version>X.Y.Z</Version>` to the bumped version.
   - Insert a new entry at the top of `<PackageReleaseNotes>`:
     ```
       X.Y.Z
       <one hyphen-prefixed line per commit subject>
     ```
   Keep all existing entries below.

7. **Commit**
   Stage `Directory.Build.props` and commit with message:
   ```
   Bump version to X.Y.Z
   ```
   Ask the user for permission first.

## Notes

- Only modify `Directory.Build.props`. Do not touch any other file.
- Preserve the exact indentation style of the existing file (4-space indent).
- If there are no commits since the last tag, abort and tell the user.
