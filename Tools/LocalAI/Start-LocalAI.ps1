[CmdletBinding()]
param(
    [string]$Python = 'python',
    [string]$RuntimeDirectory = (Join-Path $env:LOCALAPPDATA 'VKTLocalAI/runtime'),
    [switch]$Prepare,
    [switch]$Background
)
$ErrorActionPreference = 'Stop'
$runtimeRoot = [IO.Path]::GetFullPath($RuntimeDirectory)
$pythonPath = Join-Path $runtimeRoot 'Scripts/python.exe'
if (-not (Test-Path -LiteralPath $pythonPath)) {
    if (-not $Prepare) { throw 'Runtime missing. Run with -Prepare and a Python 3.12+ executable.' }
    & $Python -m venv $runtimeRoot
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated Python runtime.' }
}
if ($Prepare) {
    & $pythonPath -m pip install --disable-pip-version-check -r (Join-Path $PSScriptRoot 'requirements.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Dependency installation failed.' }
}
$arguments = @('-m','uvicorn','backend:create_app','--factory','--workers','1','--host','127.0.0.1','--port','18741','--app-dir',$PSScriptRoot,'--no-access-log','--log-level','warning')
if ($Background) {
    # Quote the known script directory for paths containing spaces; no shell evaluation.
    $arguments[$arguments.Count-4] = '"' + $PSScriptRoot + '"'
    $process = Start-Process -FilePath $pythonPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output "VKT local story process started: PID $($process.Id), http://127.0.0.1:18741/health"
} else {
    & $pythonPath @arguments
    if ($LASTEXITCODE -ne 0) { throw 'VKT local story service stopped with an error.' }
}
