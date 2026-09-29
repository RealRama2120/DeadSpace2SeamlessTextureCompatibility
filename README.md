# Dead Space 2 Seamless Texture Compatibility

Version 1.0.0. This standalone loader targets the original 32-bit Steam edition of Dead Space 2. It discovers compatible TexMod packages and starts the game through TexMod from Steam's normal Play button. See [the test report](docs/RELEASE-TEST-REPORT.md) for release details.

The mod discovers compatible TexMod `.tpf` packs on each launch, validates their contents, and starts the original game through a verified TexMod installation. Your normal Steam Play button remains the entry point. No renamed game executable, custom Steam launch options, manual TexMod setup, or dependency on the Dead Space 2008 mod is required.

## Install

Close the game. Extract the release ZIP beside `deadspace2.exe` and `DS2DAT0.DAT`. The three runtime files are `version.dll`, `DS2TextureLauncher.exe`, and `DS2SeamlessTextures.json`. Keep the documentation and license folder with them. If another mod already owns `version.dll`, stop: this loader does not chain another version proxy. Never overwrite an existing proxy to resolve a conflict.

The same ZIP has a game-root layout recognized by the separate Dead Space 2 game-support extension. Install it through that extension, enable it, and deploy. The extension is not bundled or modified. The user reported successful Vortex deployment, disable and re-enable with the earlier beta; a complete purge and uninstall recovery check remains open.

Install your texture packs separately, preferably inside `TexMod Packages` and its subfolders. The mod also checks game-root `.tpf` files, `TexturePacks`, and `Mods`. It follows Vortex file symlinks, skips directory junctions/symlinks, and ignores unrelated archives and Vortex metadata. No texture packs are included.

Launch through Steam normally. A trusted existing `Texmod.exe` in the game folder is reused. Otherwise, the first launch asks permission to download the original TexMod 0.9b from its archived project and checks pinned archive and executable hashes. Declining cancels the texture-enabled launch. Later launches are automatic.

## Compatibility and evidence

The proxy occupies `version.dll`; it preserves MarkerPatch's `DINPUT8.dll`, dgVoodoo's `D3D9.dll`, and ReShade's `DXGI.dll`. Real-game testing has confirmed a diagnostic replacement with those modules and REST loaded, plus higher-resolution gameplay texture bindings with all ten audited Return to Titan packs selected. Exact Return to Titan artwork remains unverified. Steam Overlay Shift+Tab did not open in user testing; an ordinary-game baseline is still needed. EA/Origin and other storefronts have not been tested. Consult the test report before use.

The original Steam-started process waits while one TexMod-started process renders the game. Two `deadspace2.exe` process entries are therefore expected during an enabled session; only one should render a game window. Closing the rendered game normally ends the companion and waiting parent. The Steam overlay in the rendered child remains a known validation concern.

## Configuration and troubleshooting

No configuration changes are required for ordinary use. See the included `CONFIGURATION.md` in the documentation folder for exact priorities, disabled packs, discovery folders and diagnostics.

Logs are in `%LOCALAPPDATA%\Rama2120\DS2SeamlessTextures`. Start with `latest.log` and `packages.json`; `sessions/<session>/runtime.log` records individual replacement results. `RETURN_TO_TITAN selected=N/10` in `latest.log` names missing audited packs when any are installed. `INJECTION_ATTEMPTED` only means TexMod Run was invoked. `INJECTION_VERIFIED` confirms the named encountered target's replacement pixels. `INJECTION_PIXEL_MISMATCH` means a different bound texture object did not exactly match the selected package; it does not prove that no texture substitution occurred. Unencountered textures, cubemaps, unsupported image encodings and ambiguous internal entries remain unverified.

A corrupt or inaccessible selected pack stops the texture launch with an error. Inconclusive texture-verification results, including exact-pixel mismatches, are recorded in the logs without interrupting gameplay. Only actionable launch or package failures display an error dialog. Retain logs when reporting a problem; review local paths before sharing them. See `TROUBLESHOOTING.md` for common cases.

## Disable or uninstall

To temporarily disable the mod, set `Enabled` to `false` in `DS2SeamlessTextures.json`, then launch normally. The original process continues without the TexMod handoff.

For a manual uninstall, close the game and remove only this mod's `version.dll`, `DS2TextureLauncher.exe`, `DS2SeamlessTextures.json`, and `DS2STC-docs` folder. Use the package manifest to identify them. If another tool changed one of these files, resolve ownership before removing it. For Vortex, disable this mod and deploy/purge through Vortex. Leave all texture packs, `Texmod.exe`, game files, saves, `DINPUT8.dll`, `D3D9.dll`, `DXGI.dll`, REST and Conduit files alone. Optional cached logs and the consent-downloaded TexMod copy live only inside the mod's LocalAppData folder.

## Build from source

Use Windows with Visual Studio Build Tools with the C++ x86/x64 tools, a Windows SDK, and .NET Framework 4.x (4.8 recommended). From the extracted source folder, run:

```powershell
.\build.ps1
.\tests\run.ps1
.\tests\native.ps1
```

The build creates `build/version.dll` and `build/DS2TextureLauncher.exe`. See [docs/BUILD.md](docs/BUILD.md) for detailed requirements and test notes. Project code is MIT licensed; MinHook's bundled license also applies.
