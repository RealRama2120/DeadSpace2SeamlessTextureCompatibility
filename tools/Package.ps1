param([switch]$Force,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$project=Split-Path -Parent $PSScriptRoot
$release=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $project 'release'}
New-Item -ItemType Directory -Force -Path $release | Out-Null
$base='DeadSpace2SeamlessTextureCompatibility-v1.0.0'
$install=Join-Path $release ($base+'.zip')
$source=Join-Path $release ($base+'-source.zip')
$sums=Join-Path $release ($base+'-SHA256SUMS.txt')
$outputs=@($install,$source,$sums)
foreach($output in $outputs){if((Test-Path -LiteralPath $output) -and -not $Force){throw ('Existing package; pass -Force to replace only these package outputs: '+$output)}}

function Entry([string]$relative,[string]$file){
    if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw ('Missing package input: '+$file)}
    $item=Get-Item -LiteralPath $file
    if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw ('Refusing linked package input: '+$file)}
    [pscustomobject]@{path=$relative.Replace('\','/');file=$item.FullName;bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
}
function WriteArchive([string]$target,[object[]]$entries,[string]$manifestPath){
    $ordered=@($entries|Sort-Object path)
    if(@($ordered|Group-Object {$_.path.ToLowerInvariant()}|Where-Object Count -gt 1).Count){throw 'Duplicate archive destination'}
    $manifest=[pscustomobject]@{package=$base;files=@($ordered|ForEach-Object {[pscustomobject]@{path=$_.path;bytes=$_.bytes;sha256=$_.sha256}})}
    $manifestBytes=[Text.Encoding]::UTF8.GetBytes(($manifest|ConvertTo-Json -Depth 5))
    $temp=$target+'.tmp'
    if(Test-Path -LiteralPath $temp){throw ('Unfinished package output requires review: '+$temp)}
    try {
        $archive=[IO.Compression.ZipFile]::Open($temp,[IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach($item in $ordered) {
                $zipEntry=$archive.CreateEntry($item.path,[IO.Compression.CompressionLevel]::Optimal)
                $zipEntry.LastWriteTime=[DateTimeOffset]::Parse('2026-09-24T00:00:00Z')
                $input=[IO.File]::OpenRead($item.file)
                try {$output=$zipEntry.Open();try{$input.CopyTo($output)}finally{$output.Dispose()}}finally{$input.Dispose()}
            }
            $zipEntry=$archive.CreateEntry($manifestPath,[IO.Compression.CompressionLevel]::Optimal)
            $zipEntry.LastWriteTime=[DateTimeOffset]::Parse('2026-09-24T00:00:00Z')
            $output=$zipEntry.Open();try{$output.Write($manifestBytes,0,$manifestBytes.Length)}finally{$output.Dispose()}
        }finally{$archive.Dispose()}
        $archive=[IO.Compression.ZipFile]::OpenRead($temp)
        try {
            $actual=@($archive.Entries|ForEach-Object FullName|Sort-Object)
            $expected=@($ordered|ForEach-Object path)+@($manifestPath)
            if((Compare-Object $actual ($expected|Sort-Object))){throw ('Archive path mismatch: '+$target)}
            foreach($item in $ordered){
                $zipEntry=$archive.GetEntry($item.path)
                if($zipEntry.Length -ne $item.bytes){throw ('Archive length mismatch: '+$item.path)}
                $stream=$zipEntry.Open()
                try{$sha=[Security.Cryptography.SHA256]::Create();try{$digest=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}finally{$stream.Dispose()}
                if($digest -ne $item.sha256){throw ('Archive hash mismatch: '+$item.path)}
            }
        }finally{$archive.Dispose()}
        if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target}
        Move-Item -LiteralPath $temp -Destination $target
    }catch{if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp};throw}
}

$runtime=@(
    Entry 'version.dll' (Join-Path $project 'build\version.dll')
    Entry 'DS2TextureLauncher.exe' (Join-Path $project 'build\DS2TextureLauncher.exe')
    Entry 'DS2SeamlessTextures.json' (Join-Path $project 'DS2SeamlessTextures.json')
)
$runtime+=Entry 'DS2STC-docs/INSTALL.txt' (Join-Path $project 'docs\INSTALL.txt')
$runtime+=Entry 'DS2STC-docs/LICENSE.txt' (Join-Path $project 'LICENSE.txt')
$runtime+=Entry 'DS2STC-docs/MINHOOK-LICENSE.txt' (Join-Path $project 'vendor\minhook\LICENSE.txt')
if(@($runtime|Where-Object {$_.path -match '(?i)\.tpf$|texmod\.exe$|graphics(host)?\.exe$|\.asi$'}).Count){throw 'Unexpected proprietary or diagnostic runtime payload'}
WriteArchive $install $runtime 'DS2STC-docs/MANIFEST.json'

$sources=@()
foreach($name in @('README.md','LICENSE.txt','DS2SeamlessTextures.json','build.ps1')){$sources+=Entry $name (Join-Path $project $name)}
foreach($folder in @('src','native','tests','tools','docs','vendor')){
    $root=Join-Path $project $folder
    foreach($file in Get-ChildItem -LiteralPath $root -File -Recurse){
        if($folder -eq 'tools' -and $file.Name -ne 'Package.ps1'){continue}
        if($folder -eq 'docs' -and $file.Name -in @('TEST-REPORT.md','BETA-TEST-REPORT.md','CANDIDATE-TEST-PLAN.md','LIVE-PROBE-PLAN.md')){continue}
        if($file.Extension.ToLowerInvariant() -notin @('.cs','.cpp','.h','.def','.json','.ps1','.md','.txt','.c','.cjs','.manifest')){throw ('Unexpected source file type: '+$file.FullName)}
        $relative=$file.FullName.Substring($project.Length+1).Replace('\','/')
        $sources+=Entry $relative $file.FullName
    }
}
WriteArchive $source $sources 'SOURCE-MANIFEST.json'
$lines=@($install,$source|ForEach-Object { $file=$_; ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($file)) })
[IO.File]::WriteAllLines($sums,[string[]]$lines,[Text.Encoding]::ASCII)
$outputs|ForEach-Object {Get-Item -LiteralPath $_|Select-Object Name,Length}
