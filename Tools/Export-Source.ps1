[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$Destination = 'C:/Users/jamal/Documents/VKT-PozarevacCity',
    [switch]$Execute
)
$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/')
$exportRoot = [IO.Path]::GetFullPath($Destination).TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath "$sourceRoot/Assets/CityGame" -PathType Container)) { throw 'CityGame source directory is missing.' }
if ($exportRoot.Equals($sourceRoot, [StringComparison]::OrdinalIgnoreCase) -or $exportRoot.StartsWith($sourceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Export must be outside the original project.' }
if ($Execute -and (Test-Path -LiteralPath $exportRoot)) { throw 'Destination already exists. Choose a new export directory; originals are never overwritten.' }

$files = [Collections.Generic.List[IO.FileInfo]]::new()
Get-ChildItem -LiteralPath "$sourceRoot/Assets/CityGame" -Recurse -File | Where-Object {
    ($_.DirectoryName -eq "$sourceRoot\Assets\CityGame\Resources\Models\KenneyRetro" -and $_.Name -in @('characterMedium.fbx','characterMedium.fbx.meta','idle.fbx','idle.fbx.meta','run.fbx','run.fbx.meta','humanMaleA.png','humanMaleA.png.meta','License.txt','License.txt.meta')) -or
    $_.Extension -eq '.cs' -or
    $_.FullName -in @("$sourceRoot\Assets\CityGame\Resources\StreetCharacter.shader", "$sourceRoot\Assets\CityGame\Resources\StreetCharacter.shader.meta") -or
    ($_.Name.EndsWith('.cs.meta')) -or
    $_.Name -in @('CityMissionMarker.mat', 'CityMissionMarker.mat.meta') -or
    $_.FullName -in @("$sourceRoot\Assets\CityGame\Resources\Art\BotanicalAtlas.png", "$sourceRoot\Assets\CityGame\Resources\Art\BotanicalAtlas.png.meta", "$sourceRoot\Assets\CityGame\Resources\Art\CityFacades.png", "$sourceRoot\Assets\CityGame\Resources\Art\CityFacades.png.meta", "$sourceRoot\Assets\CityGame\Resources\Maps\PozarevacStreets.json", "$sourceRoot\Assets\CityGame\Resources\Maps\PozarevacStreets.json.meta")
} | ForEach-Object { $files.Add($_) }
foreach ($folder in @('Docs', 'Tools')) {
    if (Test-Path -LiteralPath "$sourceRoot/$folder") {
        Get-ChildItem -LiteralPath "$sourceRoot/$folder" -Recurse -File | Where-Object {
            ($_.Extension -in @('.md', '.ps1', '.py', '.json') -or $_.FullName -in @("$sourceRoot\Docs\Screenshots\city-windows-20261015.png", "$sourceRoot\Docs\Screenshots\botanical-card-windows-20261015.png", "$sourceRoot\Docs\Screenshots\journal-windows-20261015.png", "$sourceRoot\Docs\Screenshots\combat-touch-windows-20261015.png", "$sourceRoot\Docs\Screenshots\map-windows-20261016.png", "$sourceRoot\Docs\Screenshots\city-expanded-touch-20261016.png", "$sourceRoot\Docs\Screenshots\actor-visual-windows-20261016.png", "$sourceRoot\Docs\Screenshots\garden-visual-windows-20261016.png", "$sourceRoot\Docs\Screenshots\perspective-Chase-windows-20261016.png", "$sourceRoot\Docs\Screenshots\perspective-Shoulder-windows-20261016.png", "$sourceRoot\Docs\Screenshots\perspective-FirstPerson-windows-20261016.png", "$sourceRoot\Docs\Screenshots\character-wardrobe-windows-20261016.png", "$sourceRoot\Docs\Screenshots\character-street-windows-20261016.png", "$sourceRoot\Docs\Screenshots\bench-training-windows-20261016.png", "$sourceRoot\Docs\Screenshots\blocklife-panel-windows-20261016.png", "$sourceRoot\Docs\Screenshots\blocklife-street-windows-20261016.png", "$sourceRoot\Tools\Launch-VKT.cmd", "$sourceRoot\Tools\LocalAI\requirements.txt", "$sourceRoot\Docs\Screenshots\contacts-localai-windows-20261017.png", "$sourceRoot\Docs\Screenshots\intro-original-windows-20261017.png", "$sourceRoot\Docs\Screenshots\hideout-atmosphere-windows-20261017.png", "$sourceRoot\Docs\Screenshots\character-atmosphere-windows-20261017.png")) -and $_.FullName -notmatch '[\\/](\.git|\.venv|\.pytest_cache|__pycache__|node_modules|models)[\\/]'
        } | ForEach-Object { $files.Add($_) }
    }
}
foreach ($name in @('README.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md', '.gitignore', 'ProjectSettings/ProjectVersion.txt')) {
    if (Test-Path -LiteralPath "$sourceRoot/$name" -PathType Leaf) { $files.Add((Get-Item -LiteralPath "$sourceRoot/$name")) }
}
# Fail before creating anything. Never print matching content or credential values.
$secretPattern = '(?i)(gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9_-]{20,}|eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----)'
foreach ($file in $files) {
    if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Symbolic link excluded: $($file.Name)" }
    if ($file.Extension -notin @('.png', '.fbx') -and [IO.File]::ReadAllText($file.FullName) -match $secretPattern) { throw "Potential credential in allowlisted file: $($file.Name). Review locally before export." }
}
$manifest = Get-Content -LiteralPath "$sourceRoot/Packages/manifest.json" -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
foreach ($id in @('com.cesium.unity', 'com.unity.inputsystem', 'com.unity.render-pipelines.universal', 'com.unity.test-framework', 'com.unity.ugui')) {
    $value = $manifest.dependencies.$id
    if (-not $value) { throw "Required package missing: $id" }
    $dependencies[$id] = $value
}
foreach ($property in $manifest.dependencies.PSObject.Properties) {
    if ($property.Name.StartsWith('com.unity.modules.')) { $dependencies[$property.Name] = $property.Value }
}
$publicManifest = [ordered]@{ dependencies = $dependencies; scopedRegistries = @($manifest.scopedRegistries | Where-Object { $_.scopes -contains 'com.cesium.unity' }) }
if (-not $Execute) {
    [pscustomobject]@{ Mode = 'Dry run'; Files = $files.Count; Destination = $exportRoot; Excluded = 'Scenes, streamed map tokens, audio/video, vendors, local settings, caches'; ReadyToCopy = $true }
    return
}
New-Item -ItemType Directory -Path $exportRoot | Out-Null
foreach ($file in $files) {
    $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
    $target = Join-Path $exportRoot $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
New-Item -ItemType Directory -Path "$exportRoot/Packages" -Force | Out-Null
$publicManifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$exportRoot/Packages/manifest.json" -Encoding UTF8
# Generate public settings from explicit non-secret values, never copy private PlayerSettings.
$productGuid = [Guid]::NewGuid().ToString('N')
@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 30
  productGUID: $productGuid
  companyName: YashimiNatuki
  productName: VKT
  defaultScreenWidth: 1280
  defaultScreenHeight: 720
  activeInputHandler: 1
"@ | Set-Content -LiteralPath "$exportRoot/ProjectSettings/ProjectSettings.asset" -Encoding UTF8
[pscustomobject]@{ Mode = 'Exported'; Files = $files.Count; Destination = $exportRoot; GitOperations = 'None' }
