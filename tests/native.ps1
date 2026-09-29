$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$build=Join-Path $project 'build'
New-Item -ItemType Directory -Force -Path (Join-Path $project 'evidence'),(Join-Path $project 'private-test') | Out-Null
$pf86=[Environment]::GetFolderPath('ProgramFilesX86')
$vs=& (Join-Path $pf86 'Microsoft Visual Studio\Installer\vswhere.exe') -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$msvc=Get-ChildItem -LiteralPath (Join-Path $vs 'VC\Tools\MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$kits=Join-Path $pf86 'Windows Kits\10'
$sdk=Get-ChildItem -LiteralPath (Join-Path $kits 'Include') -Directory | Where-Object {Test-Path (Join-Path $_.FullName 'um\Windows.h')} | Sort-Object Name -Descending | Select-Object -First 1
$priorInclude=$env:INCLUDE;$priorLib=$env:LIB
$test=Join-Path $project ('private-test\native-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $test | Out-Null
try {
    $env:INCLUDE="$($msvc.FullName)\include;$($sdk.FullName)\ucrt;$($sdk.FullName)\shared;$($sdk.FullName)\um"
    $env:LIB="$($msvc.FullName)\lib\x86;$kits\Lib\$($sdk.Name)\um\x86;$kits\Lib\$($sdk.Name)\ucrt\x86"
    $cc=Join-Path $msvc.FullName 'bin\Hostx64\x86\cl.exe'
    & $cc /nologo /MT /O2 /W4 /WX /EHsc "/Fo:$build\NativeHost.obj" "/Fe:$test\deadspace2.exe" "$PSScriptRoot\NativeHost.cpp" /link version.lib
    if($LASTEXITCODE -ne 0){throw 'Host build failed'}
    & $cc /nologo /MT /O2 /W4 /WX /EHsc "/Fo:$build\LauncherStub.obj" "/Fe:$test\DS2TextureLauncher.exe" "$PSScriptRoot\LauncherStub.cpp" /link /SUBSYSTEM:WINDOWS
    if($LASTEXITCODE -ne 0){throw 'Stub build failed'}
}finally{$env:INCLUDE=$priorInclude;$env:LIB=$priorLib}
Copy-Item -LiteralPath "$build\version.dll" -Destination "$test\version.dll"
$results=@();$originalData=$env:LOCALAPPDATA;$originalChild=$env:DS2STC_TEXMOD_CHILD;$originalExit=$env:DS2STC_TEST_EXIT
try {
    $env:LOCALAPPDATA=$test
    foreach($case in @(@{name='bypass';code=10;child='';original=$true},@{name='handoff';code=0;child='';original=$false},@{name='failure';code=1;child='';original=$false},@{name='child-recursion-guard';code=0;child='1';original=$true})) {
        if(Test-Path -LiteralPath "$test\original-ran.txt"){Remove-Item -LiteralPath "$test\original-ran.txt"}
        $env:DS2STC_TEXMOD_CHILD=$case.child;$env:DS2STC_TEST_EXIT=[string]$case.code
        $p=Start-Process -FilePath "$test\deadspace2.exe" -WorkingDirectory $test -WindowStyle Hidden -PassThru
        if(-not $p.WaitForExit(10000)){$p.Kill();throw 'Native test timed out'}
        $ran=Test-Path -LiteralPath "$test\original-ran.txt"
        if($ran -ne $case.original -or ($ran -and $p.ExitCode -ne 0)){throw ('Failed '+$case.name)}
        $results += [pscustomobject]@{test=$case.name;passed=$true;exit_code=$p.ExitCode;original_entry_ran=$ran}
    }
    Remove-Item -LiteralPath "$test\version.dll"
    $env:DS2STC_TEXMOD_CHILD=''
    $p=Start-Process -FilePath "$test\deadspace2.exe" -WorkingDirectory $test -WindowStyle Hidden -PassThru -Wait
    if($p.ExitCode -ne 0){throw 'Uninstall forwarding test failed'}
    $results += [pscustomobject]@{test='uninstall-baseline';passed=$true;exit_code=$p.ExitCode;original_entry_ran=$true}
}finally{$env:LOCALAPPDATA=$originalData;$env:DS2STC_TEXMOD_CHILD=$originalChild;$env:DS2STC_TEST_EXIT=$originalExit}
$results | ConvertTo-Json | Set-Content -LiteralPath "$project\evidence\native-tests.json"
$results | Format-Table
