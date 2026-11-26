# ──────────────────────────────────────────────────────────────
#  start.ps1 — Launch Nexo (Python backend + WPF frontend)
# ──────────────────────────────────────────────────────────────
$ErrorActionPreference = "Continue"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "  ╔═════════════════════════════════╗" -ForegroundColor DarkCyan
Write-Host "  ║        Nexo · WinIsland         ║" -ForegroundColor DarkCyan
Write-Host "  ╚═════════════════════════════════╝" -ForegroundColor DarkCyan
Write-Host ""

# ── 1. Python Virtual Environment ───────────────────────────
$BackendDir  = Join-Path $Root "src\WinIsland.Backend"
$VenvDir     = Join-Path $BackendDir ".venv"
$VenvPython  = Join-Path $VenvDir "Scripts\python.exe"
$VenvPip     = Join-Path $VenvDir "Scripts\pip.exe"
$ReqFile     = Join-Path $BackendDir "requirements.txt"

if (-not (Test-Path $VenvDir)) {
    Write-Host "[1/4] Creating Python virtual environment..." -ForegroundColor Yellow
    python -m venv $VenvDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Python 3.11+ required. https://python.org" -ForegroundColor Red
        exit 1
    }
    Write-Host "[2/4] Installing Python packages..." -ForegroundColor Yellow
    & $VenvPip install -r $ReqFile --quiet 2>$null
} else {
    Write-Host "[1/4] Python venv ✓" -ForegroundColor Green
    Write-Host "[2/4] Dependencies  ✓" -ForegroundColor Green
}

# ── 2. Start Python backend ─────────────────────────────────
Write-Host "[3/4] Starting backend service..." -ForegroundColor Yellow
$backendMain = Join-Path $BackendDir "main.py"
$backend = Start-Process -FilePath $VenvPython `
    -ArgumentList $backendMain `
    -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# ── 3. Build & launch C# frontend ───────────────────────────
$UIDir = Join-Path $Root "src\WinIsland.UI"
Write-Host "[4/4] Building & launching Nexo..." -ForegroundColor Cyan
Write-Host ""

try {
    dotnet run --project $UIDir
}
finally {
    # Clean up backend process
    if ($backend -and !$backend.HasExited) {
        Stop-Process -Id $backend.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host ""
    Write-Host "Nexo closed." -ForegroundColor Gray
}

