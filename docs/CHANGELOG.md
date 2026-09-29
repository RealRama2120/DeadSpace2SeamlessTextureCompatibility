# Changelog

## 1.0.0 — 2026-09-24

- Promoted the tested release candidate to 1.0.0 for the original Steam edition. The candidate selected all ten audited Return to Titan packs, started its in-game observer, and exited normally; the user reported that everything seemed to work well.
- Kept inconclusive texture comparisons in the log without a gameplay-interrupting warning. Package validation and launch failures still display an error. Exact Return to Titan pixels, Steam Overlay interaction, EA/Origin and full live Vortex purge recovery remain outside the demonstrated scope.

## 1.0.0-rc1 — 2026-09-24

- Reserved in-game warning dialogs for actionable package or launch failures. The runtime observer logs inconclusive pixel mismatches, unchanged objects and pending initialization without interrupting gameplay or asserting that injection failed.
- Carried forward the full ten-pack Steam gameplay result: 40 different, higher-resolution bound texture objects, including 15 targets associated with the dedicated suit pack. Two in-game collision targets bound at the selected higher-priority pack's resolution. Exact artwork remains unverified.
- Added release-candidate troubleshooting, packaging and an evidence-limited test report. A final in-game run of this binary and an interactive ordinary-game overlay comparison remain open.

## 0.5.2-beta — 2026-09-24

- Recorded the first v0.5.1 user gameplay run with all ten audited Return to Titan packs selected through normal Steam Play. The user reported smooth play and improved suit appearance; all 40 readable mismatched runtime targets bound at higher resolution than mapped originals, including 15 associated with the dedicated suit pack. Exact package artwork remains unverified.
- Inconclusive exact-pixel mismatches are now logged without interrupting gameplay with a modal warning. The launcher drains the final runtime log after game exit so its summary includes events accumulated while any stronger warning was open.
- Steam Shift+Tab did not work in the user's modded run. An interactive unmodded baseline is still needed before attributing the cause.
- v0.5.2 has passed offline checks but has not been relaunched in the game.

## 0.5.1-beta — 2026-09-24

- Corrected the runtime observer's wording: an exact-pixel mismatch in a different bound texture object is now reported as `INJECTION_PIXEL_MISMATCH`, not proof that texture injection failed.
- Added an exact-content Return to Titan pack count and missing-pack names to `latest.log` when any audited packs are selected.
- Recorded the first user gameplay session: eight packs were deployed, 52 of 53 mismatched targets bound at higher resolution than their mapped game originals, approximately 25 minutes of stable play, save/reload, and Vortex disable/re-enable. Steam overlay did not open and remains unresolved.
- No texture packs or third-party game/mod files are bundled. The corrected runtime needs a new in-game retest before stronger claims.

## 0.5.0-beta — 2026-09-24

- Carried forward the candidate's normal Steam Play evidence: all ten Return to Titan packs selected, menu rendering, save visibility, and normal exit on the tested installation. The newly labeled build has not been relaunched in-game.
- Replaced the live-test guardian's restrictive save locks with verified, closed backup copies; ordinary and mod-enabled save-menu checks now both show the existing save.
- Added a no-pack test phase for later acceptance and repaired portable ZIP paths and Windows PowerShell packaging.
- Labeled the release beta because Return to Titan gameplay replacement, extended stability, Vortex UI deployment, overlay behavior, and full uninstall recovery remain unverified.

## 0.1.0-candidate — 2026-09-23

- Introduced the standalone DS2 version proxy and automatic TexMod companion.
- Added per-launch package discovery, Vortex file-symlink support, content validation, priorities, exact disables and duplicate handling.
- Added exact-content Return to Titan order and superseded-package profiles without bundling texture content.
- Added consent-based HTTPS acquisition with pinned original TexMod checksums.
- Added executable selection and package-list readback, runtime replacement evidence and visible failure reporting.
- Added isolated package/native/graphics tests and reversible full-stack live-test tooling.
- Validation is ongoing. Consult TEST-REPORT before any public release.
