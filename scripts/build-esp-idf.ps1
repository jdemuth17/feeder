param(
    [string[]]$IdfArgs = @("build")
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $repoRoot "src\Firmware\UniversalFeeder.EspIdf"
$idfToolsPath = "C:\Espressif"
$idfPath = Join-Path $idfToolsPath "frameworks\esp-idf-v5.5.3"
$pythonEnvPath = Join-Path $idfToolsPath "python_env\idf5.5_py3.11_env"
$pythonExe = Join-Path $pythonEnvPath "Scripts\python.exe"
$idfPy = Join-Path $idfPath "tools\idf.py"

$requiredPaths = @(
    $projectRoot,
    $idfToolsPath,
    $idfPath,
    $pythonExe,
    $idfPy
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path $path)) {
        throw "Required ESP-IDF path was not found: $path"
    }
}

$toolDirs = @(
    (Join-Path $pythonEnvPath "Scripts"),
    (Join-Path $idfToolsPath "tools\cmake\3.30.2\bin"),
    (Join-Path $idfToolsPath "tools\ninja\1.12.1"),
    (Join-Path $idfToolsPath "tools\xtensa-esp-elf\esp-14.2.0_20251107\xtensa-esp-elf\bin")
) | Where-Object { Test-Path $_ }

$env:IDF_TOOLS_PATH = $idfToolsPath
$env:IDF_PATH = $idfPath
$env:IDF_PYTHON_ENV_PATH = $pythonEnvPath
$env:PATH = (($toolDirs + $env:PATH.Split(';')) | Select-Object -Unique) -join ';'

Push-Location $projectRoot
try {
    Write-Host "Using IDF_PATH=$env:IDF_PATH"
    Write-Host "Using IDF_PYTHON_ENV_PATH=$env:IDF_PYTHON_ENV_PATH"
    & $pythonExe $idfPy @IdfArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}