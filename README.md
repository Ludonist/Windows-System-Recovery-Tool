# System Restore Tool v2.5.0 — Release Binaries

This branch contains pre-built binaries for v2.5.0.
For source code, see the `main` branch.

## Downloads

| File | Size | Description |
|------|------|-------------|
| `SystemRestoreTool-standalone-win-x64.zip` | ~40 MB | x64, includes .NET, no installation required |
| `SystemRestoreTool-standalone-win-x86.zip` | ~37 MB | x86, for older 32-bit systems |
| `SystemRestoreTool-standalone-win-arm64.zip` | ~33 MB | ARM64 (Surface Pro X, Snapdragon) |
| `SystemRestoreTool-compact-win-x64.zip` | ~7 MB | x64, requires .NET 6 Runtime |
| `SystemRestoreTool-compact-win-x86.zip` | ~7 MB | x86, compact version |
| `SystemRestoreTool-compact-win-arm64.zip` | ~6 MB | ARM64, compact version |

## Verification

Verify SHA256 checksums:
```cmd
certutil -hashfile SystemRestoreTool-standalone-win-x64.zip SHA256
```

Compare with `SHA256SUMS.txt`.

## Usage

1. Download the appropriate ZIP for your architecture
2. Extract `SystemRestoreTool.exe`
3. Right-click → **Run as administrator**
4. Use the menu (67 items, 3 languages: RU/EN/ZH)

## Source Code

See `main` branch: https://github.com/Ludonist/Windows-System-Recovery-Tool
