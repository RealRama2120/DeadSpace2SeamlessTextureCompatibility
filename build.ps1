param([switch]$NativeOnly)
$ErrorActionPreference='Stop'
$project=$PSScriptRoot
$build=Join-Path $project 'build'
New-Item -ItemType Directory -Force -Path $build | Out-Null
$pf86=[Environment]::GetFolderPath('ProgramFilesX86')
$vswhere=Join-Path $pf86 'Microsoft Visual Studio\Installer\vswhere.exe'
$vs=& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$msvc=Get-ChildItem -LiteralPath (Join-Path $vs 'VC\Tools\MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$kits=Join-Path $pf86 'Windows Kits\10'
$sdk=Get-ChildItem -LiteralPath (Join-Path $kits 'Include') -Directory | Where-Object {Test-Path (Join-Path $_.FullName 'um\Windows.h')} | Sort-Object Name -Descending | Select-Object -First 1
$priorInclude=$env:INCLUDE;$priorLib=$env:LIB
try {
    $env:INCLUDE="$($msvc.FullName)\include;$($sdk.FullName)\ucrt;$($sdk.FullName)\shared;$($sdk.FullName)\um"
    $env:LIB="$($msvc.FullName)\lib\x86;$kits\Lib\$($sdk.Name)\um\x86;$kits\Lib\$($sdk.Name)\ucrt\x86"
    & (Join-Path $msvc.FullName 'bin\Hostx64\x86\cl.exe') /nologo /LD /MT /std:c++17 /O2 /W4 /WX /EHsc /DUNICODE /D_UNICODE "/Fo:$build\Bootstrap.obj" "/Fe:$build\DS2SeamlessTextures.asi" "$project\native\Bootstrap.cpp" /link /DYNAMICBASE /NXCOMPAT advapi32.lib user32.lib
    if($LASTEXITCODE -ne 0){throw 'Native build failed'}
    $compiler=Join-Path $msvc.FullName 'bin\Hostx64\x86\cl.exe'
    & $compiler /nologo /MT /O2 /W4 /WX /EHsc /std:c++17 "/Fo:$build\GraphicsHost.obj" "/Fe:$build\GraphicsHost.exe" "$project\tests\GraphicsHost.cpp" /link user32.lib version.lib d3d9.lib
    if($LASTEXITCODE -ne 0){throw 'Graphics test host compilation failed'}
    foreach($file in @('buffer.c','hook.c','trampoline.c','hde\hde32.c')) {
        $name=[IO.Path]::GetFileNameWithoutExtension($file)
        & $compiler /nologo /c /MT /O2 "/Fo:$build\$name.obj" "$project\vendor\minhook\src\$file"
        if($LASTEXITCODE -ne 0){throw 'MinHook compilation failed'}
    }
    & $compiler /nologo /LD /MT /O2 /W4 /WX /EHsc /std:c++17 "/Fo:$build\VersionBootstrap.obj" "/Fe:$build\version.dll" "$project\native\VersionBootstrap.cpp" "$build\buffer.obj" "$build\hook.obj" "$build\trampoline.obj" "$build\hde32.obj" /link "/DEF:$project\native\version.def" /DYNAMICBASE /NXCOMPAT user32.lib
    if($LASTEXITCODE -ne 0){throw 'Version bootstrap compilation failed'}
}finally{$env:INCLUDE=$priorInclude;$env:LIB=$priorLib}
if(-not $NativeOnly) {
    $cs=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    & $cs /nologo /target:winexe /platform:x86 /optimize+ /checked+ /warn:4 /warnaserror+ "/win32manifest:$project\src\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll "/out:$build\DS2TextureLauncher.exe" (Get-ChildItem "$project\src\*.cs" | ForEach-Object FullName)
    if($LASTEXITCODE -ne 0){throw 'Launcher build failed'}
}
