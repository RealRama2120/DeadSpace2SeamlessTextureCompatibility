# Dead Space 2 Seamless Texture Compatibility — v1.0.0

Install compatible TexMod `.tpf` packs, then start the original 32-bit Steam Dead Space 2 through Play. This standalone loader finds packs on each launch, validates them and starts the game through TexMod automatically. No renamed game executable, Steam launch option or separate daily launcher is needed. Exact package-priority and disable rules are available for conflicts.

**Evidence and scope:** A controlled diagnostic texture was visibly replaced in Dead Space 2 through normal Steam Play. A user gameplay run selected all ten audited Return to Titan packs and ran smoothly; the user perceived improved suits. The runtime bound 40 different readable textures at higher resolution than mapped game originals, including 15 targets associated with the dedicated suit pack. Two colliding targets bound at the selected higher-priority pack's resolution. The user then tested the 1.0.0 release candidate in the game and reported that everything seemed to work well; its log selected all ten packs and recorded a normal exit. Exact Return to Titan pixels were not confirmed, so the observer logs that uncertainty without interrupting play. Shift+Tab did not open Steam Overlay in an earlier modded user test; ordinary-game behavior has not been compared interactively. EA/Origin and other storefronts have not been tested. See `DS2STC-docs/TEST-REPORT.md` in the ZIP.

## Installation

Close the game. For manual installation, extract the ZIP into the folder containing `deadspace2.exe` and `DS2DAT0.DAT`. It places `version.dll`, `DS2TextureLauncher.exe`, `DS2SeamlessTextures.json` and a documentation folder at the game root. If `version.dll` already exists, resolve that conflict first; do not overwrite another mod's version proxy.

The same ZIP has been checked against the separate Dead Space 2 Vortex game-support extension's installer mapping. The user reported that disabling, re-enabling and deploying the earlier beta through Vortex worked; the ten-pack run also used its deployed file links. A complete uninstall/purge recovery check remains open.

Install texture packs separately, for example under `TexMod Packages`. Game-root `.tpf` files and subfolders of `TexMod Packages`, `TexturePacks` and `Mods` are scanned on every launch. The ZIP contains no texture packs. Known obsolete Return to Titan packages are excluded only when their exact audited replacement content is present.

Launch through Steam normally. The mod reuses a checksum-verified original `Texmod.exe` if available. Otherwise, first launch asks consent to retrieve the archived TexMod 0.9b over HTTPS and verifies pinned hashes. Declining cancels the texture-enabled launch. TexMod itself is not bundled.

## Disable, logs and removal

Set `Enabled` to `false` in `DS2SeamlessTextures.json` to bypass the texture handoff. Logs are under `%LOCALAPPDATA%\Rama2120\DS2SeamlessTextures`. `RETURN_TO_TITAN selected=N/10` names missing audited packs. `INJECTION_ATTEMPTED` confirms only that TexMod Run was invoked; `INJECTION_VERIFIED` names a texture whose replacement pixels matched. `INJECTION_PIXEL_MISMATCH` means exact package pixels could not be confirmed in a different bound texture object, not that no replacement occurred. It is logged without a gameplay-interrupting dialog.

For manual removal, close the game and remove only this mod's `version.dll`, `DS2TextureLauncher.exe`, `DS2SeamlessTextures.json` and `DS2STC-docs`, after checking the included manifest. Do not remove TexMod, texture packs, other proxy DLLs, game files or saves. With Vortex, disable and purge this mod through Vortex. Complete live uninstall recovery has not yet been observed.

The ZIP includes the project MIT license and MinHook attribution. A separate source ZIP is available. TexMod, Return to Titan, MarkerPatch, dgVoodoo, ReShade, REST and Conduit assets are not redistributed. No public source repository is claimed.
