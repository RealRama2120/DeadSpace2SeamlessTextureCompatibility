param([ValidateSet('system','dgvoodoo')][string]$Backend='system',[switch]$Reverse,[switch]$Trace,[switch]$Baseline,[switch]$Reference)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$root=Join-Path $project ('private-test\graphics-'+$Backend+'-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $root 'TexMod Packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'build\GraphicsHost.exe') -Destination (Join-Path $root 'deadspace2.exe')
New-Item -ItemType File -Path (Join-Path $root 'DS2DAT0.DAT') | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'private-test\game\Texmod.exe') -Destination (Join-Path $root 'Texmod.exe')
if($Backend -eq 'dgvoodoo'){Copy-Item -LiteralPath (Join-Path $project 'private-test\game\D3D9.dll') -Destination (Join-Path $root 'D3D9.dll')}
if($Trace){Copy-Item -LiteralPath (Join-Path $project 'build\version.dll') -Destination (Join-Path $root 'version.dll')}
$test=(Get-Content -LiteralPath (Join-Path $project 'evidence\latest-test-root.txt')).Trim()
if($Reference){Copy-Item -LiteralPath (Join-Path $project 'private-test\Reference-Magenta.tpf') -Destination (Join-Path $root 'TexMod Packages')}else{Copy-Item -Path (Join-Path $test 'graphics-packages\*.tpf') -Destination (Join-Path $root 'TexMod Packages')}
$settings=Get-Content -LiteralPath (Join-Path $project 'DS2SeamlessTextures.json') -Raw | ConvertFrom-Json
$settings.Priority=[pscustomobject]@{'TexMod Packages/Probe-Magenta.tpf'=$(if($Reverse){-100}else{100})}
$settings | Add-Member -NotePropertyName TraceTextures -NotePropertyValue ([bool]$Trace) -Force
$settings | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'DS2SeamlessTextures.json')
if($Baseline){$process=Start-Process -FilePath (Join-Path $root 'deadspace2.exe') -WorkingDirectory $root -WindowStyle Hidden -PassThru}
else {$process=Start-Process -FilePath (Join-Path $project 'build\DS2TextureLauncher.exe') -ArgumentList @('--game-root',('"'+$root+'"'),'--data-root',('"'+(Join-Path $root 'logs')+'"')) -WorkingDirectory $root -WindowStyle Hidden -PassThru}
if(-not $process.WaitForExit(90000)){throw ('Graphics test timeout; inspect owned processes under '+$root)}
$record=[pscustomobject]@{backend=$Backend;reverse=[bool]$Reverse;trace=[bool]$Trace;baseline=[bool]$Baseline;root=$root;exit_code=$process.ExitCode;result=$(if(Test-Path -LiteralPath (Join-Path $root 'graphics-result.txt')){Get-Content -LiteralPath (Join-Path $root 'graphics-result.txt')})}
$record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $project ('evidence\graphics-'+$Backend+'-'+[bool]$Reverse+'-'+[bool]$Trace+'-'+[bool]$Baseline+'-reference'+[bool]$Reference+'.json'))
$record | ConvertTo-Json
