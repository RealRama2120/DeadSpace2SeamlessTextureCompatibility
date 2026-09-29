# Configuration

`DS2SeamlessTextures.json` sits beside the game executable. A missing file uses the same built-in defaults. Close the game before changing it; deployment and package content are locked during a session to avoid a mixed launch.

| Setting | Default | Meaning |
| --- | --- | --- |
| Enabled | true | false returns to the original launch path |
| RecognizeReturnToTitan | true | recognize only the exact audited package content hashes |
| TrustedTexModPath | empty | optional absolute path to original, checksum-verified TexMod |
| PackageFolders | TexMod Packages, TexturePacks, Mods | relative folders inside the game root, searched recursively |
| Priority | empty object | integer priorities keyed by exact relative path or `sha256:<64 hex digits>` |
| Disabled | empty list | exact relative paths or SHA-256 rules to exclude |
| ShowTexMod | false | true exposes the TexMod window for troubleshooting |
| UiDelayMs | 250 | automation delay, clamped to 100–3000 ms |
| TraceTextures | false | optional verbose texture upload trace; produces large logs |

Game-root TPF files are always scanned. Directory reparse points are skipped to avoid loops; regular deployed file symlinks are supported. Folder enumeration failures stop the launch. Packs are never automatically extracted or rewritten.

Example optional overrides:

```json
{
  "Enabled": true,
  "RecognizeReturnToTitan": true,
  "Priority": {"TexMod Packages/My Pack.tpf": 2000},
  "Disabled": ["TexMod Packages/Old Pack.tpf"]
}
```

Higher numbers appear nearer the top of TexMod's list. The top row wins in both tested system D3D9 and dgVoodoo controlled collision tests. Equal priorities sort by relative path, ignoring case. Avoid assigning conflicting path and hash rules to the same pack. Different packs with the same filename remain distinct; byte-identical copies collapse to the highest-priority copy.

The recognized Return to Titan order, highest first, is: `1-4KMainSuitsnew`, `1WEP_RTTn`, `3INTERACTABLES_RTTn`, `4INTERACTABLES_RTTn`, `5INTERACTABLES_RTTn`, `6INTERACTABLES_RTT`, `7INTERACTABLES_RTT`, `8INTERACTABLES_RTT`, `9INTERACTABLES_RTT`, `2Kaio`. These receive priorities 1000 through 991. Generic packs default to 0; use a value above 1000 when you intentionally want another pack to win. This order is a built-in preference for the audited identities, not a claim that every collision has been encountered in gameplay.

Only the exact audited old weapon, third interactables and fourth interactables packages can be marked superseded. Their exact replacement hashes must also be selected; the old third package requires both current third and sixth packages. Renaming a pack does not change its identity. Unknown updates and similar names do not trigger automatic exclusion.

Diagnostics for developers: `DS2TextureLauncher.exe --scan-only --game-root <folder> --data-root <log-folder>` checks discovery and integrity without starting TexMod. `--prepare-only` additionally selects the EXE and submits/reads back the package list, but does not press Run. These are optional developer tools, not installation steps.

For a quick Return to Titan check, open `%LOCALAPPDATA%\Rama2120\DS2SeamlessTextures\latest.log` after launch and find `RETURN_TO_TITAN selected=N/10`. It lists the names of any missing audited packs. A partial set is informational; the loader does not silently install or redistribute missing texture files.
