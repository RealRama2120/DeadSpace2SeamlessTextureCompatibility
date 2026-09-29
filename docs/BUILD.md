# Build from source

Use Windows with Visual Studio Build Tools' C++ x86/x64 tools, a Windows 10/11 SDK, and .NET Framework 4.x (4.8 recommended). The build finds Visual Studio through `vswhere.exe` and uses the installed Framework C# compiler. The game and launcher are 32-bit; building on 64-bit Windows is supported.

From the extracted source directory, run:

```powershell
.\build.ps1
.\tests\run.ps1
.\tests\native.ps1
```

The runtime outputs are `build/version.dll` and `build/DS2TextureLauncher.exe`. Native code links the static C runtime and includes MinHook from `vendor/minhook`; the companion uses Windows' .NET Framework. Compiler warnings in project C++ and C# are errors. `build.ps1 -NativeOnly` skips rebuilding the companion.

`DS2SeamlessTextures.asi` and `GraphicsHost.exe` are diagnostic outputs only and must never be placed in the runtime release archive. The ASI was investigated as a possible bootstrap; the chosen runtime bootstrap is the version proxy.

The build is reproducible from the supplied source and installed toolchain without fetching source dependencies. Byte-for-byte output identity across compiler/SDK versions is not promised. Record compiler versions and hashes when comparing a rebuild.

`tests/run.ps1` creates its own encrypted TPF fixtures in a fresh isolated directory. `tests/native.ps1` creates a mock game and companion to test return codes, forwarding, recursion prevention and removal. Neither establishes actual-game compatibility.

`tests/graphics.ps1` is a local developer integration harness. It requires a separately acquired, verified TexMod copy and optionally dgVoodoo. Its default local input paths reflect this validation workspace and should be adapted for another machine. The proprietary copies are deliberately excluded from the source archive. The test renders a known texture, reads the bound replacement and framebuffer, and reverses conflicting pack priority. `-Trace` includes the runtime observer.

Machine-specific live-test scripts and private evidence are retained in the development workspace, not distributed in the source ZIP. They are not required to build the runtime.

Run `tools/Package.ps1` to generate the shared manual/Vortex-layout release ZIP, source ZIP, manifests and SHA-256 sums. Packaging does not establish gameplay or Vortex UI acceptance.
