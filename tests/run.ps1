$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path (Join-Path $project 'build'),(Join-Path $project 'evidence'),(Join-Path $project 'private-test') | Out-Null
& $csc /nologo /target:exe /main:Tests /platform:x86 /optimize+ /checked+ /warnaserror+ /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll "/out:$project\build\tests.exe" "$PSScriptRoot\Tests.cs" (Get-ChildItem "$project\src\*.cs" | ForEach-Object FullName)
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
Copy-Item -LiteralPath "$PSScriptRoot\dds-golden.json" -Destination "$project\build\dds-golden.json"
$temp=Join-Path $project ('private-test\unit-'+[Guid]::NewGuid().ToString('N'))
& "$project\build\tests.exe" $temp | Tee-Object -FilePath "$project\evidence\unit-tests.txt"
if($LASTEXITCODE -ne 0){throw 'Tests failed'}
$temp | Set-Content -LiteralPath "$project\evidence\latest-test-root.txt"
