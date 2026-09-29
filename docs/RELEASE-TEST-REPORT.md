# 1.0.0 validation report

This release targets the original 32-bit Steam edition of Dead Space 2. Results distinguish the beta gameplay runs, the in-game 1.0.0-rc1 acceptance run, and checks of the final 1.0.0 archive. The final build changes only version metadata and documentation from the tested candidate. EA/Origin, other storefronts and a second device have not been tested.

## Demonstrated in Dead Space 2

- Steam's normal Play button started the texture launcher and a single rendered TexMod child without renaming `deadspace2.exe` or setting Steam launch options. The waiting Steam parent and helper ended after a normal user exit; no orphaned game or TexMod process remained in the checked runs.
- A controlled diagnostic TPF visibly changed a title-menu texture and the runtime observer matched its bound replacement pixels. This proves one real game replacement, not universal compatibility.
- The existing save appeared in equivalent ordinary and mod-enabled guarded menu tests. Both ended normally and protected game, graphics/mod, save and settings files matched their pre-run hashes. The user later reported successful save/reload during an eight-pack gameplay run; no independent post-run save hash was recorded for that playthrough.
- The user reported approximately 25 minutes of smooth eight-pack gameplay, no missing or flickering textures, working controls and ReShade, normal exit/relaunch, and Vortex disable/re-enable/deploy. These observations were not a full ten-pack stress test.
- In a subsequent ten-pack user gameplay run, `latest.log` selected all ten exact audited Return to Titan packages and listed them in the intended order. The runtime observer initialized in the rendered process with 396 expected hashes and a successful hook. The user reported smooth play and improved-looking suits, then exited normally.
- The raw ten-pack runtime log recorded 40 different readable bound texture objects at the selected pack's dimensions and formats. All 40 were higher resolution than their mapped original game textures. Fifteen target hashes were first claimed by the dedicated suit pack; nine were first claimed by the AIO pack. Two targets shared by packages with differing resolutions bound at the higher-priority pack's resolution, supporting the configured collision order in-game.
- The user tested 1.0.0-rc1 in the game and reported that everything seemed to work well. Its log selected all ten audited packs, started the observer in the rendered game with 396 expected hashes and a successful hook, recorded 32 different readable bound objects without a modal texture-warning event, and logged its complete 32-event summary after a normal exit. The rendered session lasted about four minutes; no game, TexMod or launcher process remained at inspection. This run is evidence for the new warning behavior, not a 20–30 minute stress test.

## Meaning of the texture evidence

None of the 40 ten-pack bound objects exactly matched the selected TPF pixels under the current observer. The observed higher resolutions and different objects strongly support gameplay texture substitution and the user's visual report, but exact artwork and the cause of the pixel differences remain unverified. No actual CRC matched another selected pack's expected CRC for the same target. Seven other encountered targets were unsupported or ambiguous for pixel comparison. Targets not encountered in the tested scenes remain unobserved.

The v0.5.1 diagnostic popup interrupted gameplay and paused the launcher's log reader, causing its summary to count four mismatches while the raw runtime log held 40. The 1.0.0-rc1 candidate logs inconclusive observer results without a modal warning and drains the log after exit. The candidate's game run produced 32 such informational entries and a matching final summary; the user reported that it worked well. Actual package validation and launch failures still show an error. This UI/logging change does not alter texture data.

## Remaining limits

Shift+Tab did not open Steam Overlay in two user modded runs. The user recalls the same behavior without TexMod, but an interactive ordinary-game baseline has not been measured; no cause is assigned. The earlier mod-absent process loaded the overlay module, which does not by itself prove the overlay UI worked. Alt+Tab issues were also reported without TexMod.

Extended full ten-pack gameplay across several areas, complete live Vortex purge/uninstall recovery, first-run acquisition on another machine, exact Return to Titan artwork, and every package collision remain unverified. The release must not claim those checks passed. Steam is the only storefront with actual launch evidence.

## Automated and package checks

The managed suite covers TPF decoding, discovery, path safety, duplicates, priorities, disabled packs, corruption and bounded log reading. The native harness checks bypass, handoff, failure, recursion prevention and proxy removal in simulated hosts. Separate controlled graphics-host runs established top-row collision priority with system D3D9 and dgVoodoo; a later graphics-host attempt on this desktop was inconclusive. These are not actual-game proofs. Release archive entry names, sizes and SHA-256 values, source rebuild, Vortex installer mapping, third-party exclusions and personal-data screening are checked separately before delivery.
