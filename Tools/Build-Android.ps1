#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$UnityPath,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent),
    [ValidateRange(1,16)][int]$Threads = 4,
    [switch]$UseWindowsRootTrust
)
$ErrorActionPreference = 'Stop'
$vktProject = [IO.Path]::GetFullPath($ProjectRoot)
$vktOutput = [IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw 'Unity executable is missing.' }
if (-not (Test-Path -LiteralPath "$vktProject/Assets/CityGame")) { throw 'VKT source project is missing.' }
if (Test-Path -LiteralPath "$vktOutput/VKT.apk") { throw 'Output already contains VKT.apk; choose a fresh output directory.' }
if ($UseWindowsRootTrust -and [Environment]::OSVersion.Platform -ne 'Win32NT') { throw 'Windows root trust requires Windows.' }
New-Item -ItemType Directory -Path $vktOutput -Force | Out-Null
$vktGradle = Join-Path $vktOutput ('GradleProject-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
$vktUnityLog = Join-Path $vktOutput 'unity-export.log'
$vktPreviousThreads = $env:BEE_BUILD_THREADS
try {
    $env:BEE_BUILD_THREADS = [string]$Threads
    $vktArguments = @('-batchmode','-nographics','-quit','-job-worker-count',[string]$Threads,'-projectPath',('"'+$vktProject+'"'),'-buildTarget','Android','-executeMethod','Pozarevac.CityGame.Editor.CityPortableBuild.ExportAndroidProject','-vktOutput',('"'+$vktGradle+'"'),'-logFile',('"'+$vktUnityLog+'"'))
    if ($UseWindowsRootTrust) { $vktArguments += '-vktWindowsRootTrust' }
    Write-Output 'VKT Android: exporting the native ARM64 Unity project.'
    $vktUnity = Start-Process -FilePath $UnityPath -ArgumentList $vktArguments -WindowStyle Hidden -PassThru
    $vktUnity.WaitForExit()
    if ($vktUnity.ExitCode -ne 0) { throw "Unity export failed. Inspect $vktUnityLog" }
} finally { $env:BEE_BUILD_THREADS = $vktPreviousThreads }
if (-not (Test-Path -LiteralPath "$vktGradle/settings.gradle")) { throw 'Unity did not export a Gradle project.' }
$vktAndroidTools = Join-Path (Split-Path $UnityPath -Parent) 'Data/PlaybackEngines/AndroidPlayer'
$vktJava = Join-Path $vktAndroidTools 'OpenJDK/bin/java.exe'
$vktLauncher = @(Get-ChildItem -LiteralPath "$vktAndroidTools/Tools/gradle/lib" -Filter 'gradle-launcher-*.jar')
if (-not (Test-Path -LiteralPath $vktJava) -or $vktLauncher.Count -ne 1) { throw 'Bundled Android JDK/Gradle installation is incomplete.' }
$vktFlags = @()
if ($UseWindowsRootTrust) { $vktFlags = @('-Djavax.net.ssl.trustStoreType=Windows-ROOT','-Djavax.net.ssl.trustStore=NONE') }
$vktJvm = '-Xmx2048m' + $(if ($vktFlags.Count) { ' ' + ($vktFlags -join ' ') } else { '' })
$vktJavaArgs = @($vktFlags) + @('-classpath',$vktLauncher[0].FullName,'org.gradle.launcher.GradleMain',('-Dorg.gradle.jvmargs='+$vktJvm),'--no-daemon','assembleDebug')
$vktStart = [Diagnostics.ProcessStartInfo]::new()
$vktStart.FileName = $vktJava; $vktStart.WorkingDirectory = $vktGradle
$vktStart.UseShellExecute = $false; $vktStart.CreateNoWindow = $true
$vktStart.RedirectStandardOutput = $true; $vktStart.RedirectStandardError = $true
foreach ($vktArgument in $vktJavaArgs) { $vktStart.ArgumentList.Add($vktArgument) }
Write-Output 'VKT Android: assembling the APK with explicit per-build JVM settings.'
$vktProcess = [Diagnostics.Process]::Start($vktStart)
$vktStdout = $vktProcess.StandardOutput.ReadToEndAsync(); $vktStderr = $vktProcess.StandardError.ReadToEndAsync()
$vktProcess.WaitForExit()
[IO.File]::WriteAllText((Join-Path $vktOutput 'gradle-build.log'), $vktStdout.GetAwaiter().GetResult() + [Environment]::NewLine + $vktStderr.GetAwaiter().GetResult())
if ($vktProcess.ExitCode -ne 0) { throw "Gradle failed. Inspect $vktOutput/gradle-build.log" }
$vktApks = @(Get-ChildItem -LiteralPath "$vktGradle/launcher/build/outputs/apk" -Filter '*.apk' -Recurse -File)
if ($vktApks.Count -ne 1) { throw 'Expected one APK from the ARM64 debug build.' }
Copy-Item -LiteralPath $vktApks[0].FullName -Destination "$vktOutput/VKT.apk"
Get-FileHash -LiteralPath "$vktOutput/VKT.apk" -Algorithm SHA256 | Select-Object Algorithm,Hash,Path
