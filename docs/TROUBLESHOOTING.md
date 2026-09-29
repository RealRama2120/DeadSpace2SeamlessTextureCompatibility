# Troubleshooting

The mod targets the original 32-bit Steam release of Dead Space 2. Start it with Steam's normal Play button. For diagnostic details, open `%LOCALAPPDATA%\Rama2120\DS2SeamlessTextures\latest.log`; `packages.json` lists every discovered and selected TPF. These files can contain local paths, so review them before sharing.

## Return to Titan shows fewer than ten packs

The author's 0.6 base download contains `1-4KMainSuitsnew.tpf` and `2Kaio.tpf`. The 0.8 update contains the other eight current packs. Install those ten through Vortex or manually under `TexMod Packages`, and leave the three older `1WEP_RTT.tpf`, `3INTERACTABLES_RTI.tpf`, and `4INTERACTABLES_RTI.tpf` inactive. After launching, look for `RETURN_TO_TITAN selected=10/10 missing=none` in `latest.log`. This count confirms package selection, not every rendered replacement.

## A texture comparison did not match

`INJECTION_PIXEL_MISMATCH` means the game bound a different texture object at the selected pack's dimensions and format, but the observer could not match its pixels exactly. It is informational and does not prove injection failure. The loader does not interrupt gameplay for this result. `INJECTION_VERIFIED` means one encountered replacement did match exactly. Unsupported or unencountered targets remain unverified; do not interpret an absence of verification as a failed replacement.

## An error stops the texture-enabled launch

Read the error dialog and `latest.log`. A corrupt or inaccessible selected TPF, missing game executable, invalid executable architecture, TexMod acquisition failure or failed TexMod automation is actionable. Close the game and TexMod before correcting a deployment. Do not replace the game's executable or other mods' DLLs to work around an error. If another mod owns `version.dll`, resolve that file conflict before installation.

## Steam Overlay or Alt+Tab does not work

Shift+Tab did not open the overlay in a user test with this mod. Its behavior in an otherwise ordinary launch has not been compared interactively, so the cause is unresolved. Alt+Tab problems were also reported without TexMod. These observations are not evidence that a particular companion mod caused the issue. Record whether each problem also occurs with this loader disabled before changing graphics or input settings.

## Vortex shows a yellow triangle beside this archive

When installed from a local ZIP, Vortex may warn that no source is assigned. That is update-tracking metadata, not a texture or deployment error. Setting the source to `Other` removes that warning. Other Vortex warning messages should be read separately.

## Disable or remove

Set `Enabled` to `false` in `DS2SeamlessTextures.json` to bypass the texture handoff. For Vortex removal, disable and deploy/purge this mod. For manual removal, close the game, then use `DS2STC-docs/MANIFEST.json` to identify this mod's `version.dll`, `DS2TextureLauncher.exe`, `DS2SeamlessTextures.json` and documentation folder. Leave texture packs, TexMod, saves, MarkerPatch, dgVoodoo, ReShade, REST and Conduit files alone.
