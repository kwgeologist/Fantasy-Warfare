# Gate 0 Windows player package

[Download Fantasy-Warfare-Gate0-Windows-x64.zip](https://github.com/kwgeologist/Fantasy-Warfare/blob/main/packages/Fantasy-Warfare-Gate0-Windows-x64.zip?raw=true).

Extract **all** files and double-click `FantasyWarfare.exe`. No Godot, .NET SDK or separate runtime installation is required. This is the executable package, distinct from GitHub's source archive.

Windows 10/11 x64 and OpenGL 3.3 graphics drivers are required. This unsigned build passed its core/integration tests under Wine; native Windows/GPU acceptance is pending. Read the included `README-WINDOWS.txt`; optional `Verify-Gate0.cmd` runs the preserved automated tests without development tools.

The SHA-256 checksum is in the adjacent `.sha256` file. PowerShell verification: `Get-FileHash .\Fantasy-Warfare-Gate0-Windows-x64.zip -Algorithm SHA256`.

See [validation/build notes](../docs/WINDOWS_BUILD.md) and `tools/export-windows.sh` for reproduction. Gameplay and original tests remain at Gate 0. No Gate 1 features are included.
