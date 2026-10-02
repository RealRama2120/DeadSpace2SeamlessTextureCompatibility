# Dead Space 2 Seamless Texture Compatibility

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)
![Game](https://img.shields.io/badge/game-Dead%20Space%202-c41e1f)

**A seamless texture-loading mod for the original Dead Space 2 on PC.**
It discovers compatible TexMod texture packs and starts the game through
TexMod from Steam's normal Play button. No renamed game executable, no custom
Steam launch options, no manual TexMod setup, and no dependency on the Dead
Space 2008 mod.

## What it does

- Discovers compatible TexMod `.tpf` packs on each launch and validates their
  contents before loading.
- Starts the original game through a verified TexMod installation — your
  normal Steam Play button remains the entry point.
- A trusted existing `Texmod.exe` in the game folder is reused. Otherwise,
  the first launch asks your permission to download the original TexMod 0.9b
  from its archived project and checks pinned hashes. Declining cancels the
  texture-enabled launch; later launches are automatic.

## Install

Close the game. Extract the release ZIP beside `deadspace2.exe`. The three
runtime files are `version.dll`, `DS2TextureLauncher.exe`, and
`DS2SeamlessTextures.json`. If another mod already owns `version.dll`, stop:
this loader does not chain another version proxy.

Install your texture packs separately, preferably inside `TexMod Packages`
and its subfolders. Launch through Steam normally.

See the included documentation folder for full installation details. No
texture packs are included.

## Compatibility

The proxy occupies `version.dll`; it preserves MarkerPatch's `DINPUT8.dll`,
dgVoodoo's `D3D9.dll`, and ReShade's `DXGI.dll`. See
[the test report](docs/RELEASE-TEST-REPORT.md) for release details and known
validation concerns (Steam Overlay, EA/Origin storefronts).

## Configuration and troubleshooting

No configuration changes are required for ordinary use. Logs are in
`%LOCALAPPDATA%\Rama2120\DS2SeamlessTextures` — start with `latest.log`.
See `TROUBLESHOOTING.md` in the documentation folder for common cases.

To disable, set `Enabled` to `false` in `DS2SeamlessTextures.json`. For
uninstall, remove only this mod's `version.dll`, `DS2TextureLauncher.exe`,
`DS2SeamlessTextures.json`, and docs folder.

## Build from source

On Windows with Visual Studio Build Tools (C++ x86/x64), a Windows SDK, and
.NET Framework 4.x (4.8 recommended), run:

```powershell
.\build.ps1
.\tests\run.ps1
.\tests\native.ps1
```

See [docs/BUILD.md](docs/BUILD.md) for detailed requirements and test notes.

## Credits and licensing

Created by Rama2120.

Project code is MIT licensed — see [LICENSE.txt](LICENSE.txt). MinHook's
bundled license also applies.
