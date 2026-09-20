$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

Write-Host "DAEDALUS PROJECT VALIDATION" -ForegroundColor Cyan

$projectFile = Join-Path $projectRoot "DOTNETCSHARP.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: DOTNETCSHARP.csproj was not found." -ForegroundColor Red
    exit 1
}

$requiredDirectories = @(
    "game",
    "game\State",
    "ui",
    ".github",
    ".github\workflows"
)

foreach ($relativePath in $requiredDirectories) {
    if (-not (Test-Path (Join-Path $projectRoot $relativePath))) {
        Write-Host "MISSING: $relativePath" -ForegroundColor Red
        exit 1
    }
}

$requiredFiles = @(
    "Program.cs",
    "DOTNETCSHARP.csproj",
    "daedalus.meta",
    "game\GameObject.cs",
    "game\GameWorld.cs",
    "game\GameLogic.cs",
    "game\DungeonGenerator.cs",
    "game\Tile.cs",
    "game\TileRenderer.cs",
    "game\GameState.cs",
    "game\State\CampaignState.cs",
    "game\State\ExpeditionState.cs",
    "game\State\StateModels.cs",
    "game\State\SaveSystem.cs",
    "game\State\GameStateManager.cs",
    "ui\BattleSystem.cs",
    "ui\GameWindow.cs",
    "ui\GameRenderer.cs",
    "ui\ExplorationRenderer.cs",
    "ui\BattleRenderer.cs",
    "ui\StatsWindow.cs",
    ".github\workflows\build.yml"
)

foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path (Join-Path $projectRoot $relativePath))) {
        Write-Host "MISSING: $relativePath" -ForegroundColor Red
        exit 1
    }
}

$tileRendererContent = Get-Content (Join-Path $projectRoot "game\TileRenderer.cs") -Raw
foreach ($requirement in @("class TileRenderer", "static void Draw", "GetTileColor", "TileType.Wall")) {
    if ($tileRendererContent -notmatch [regex]::Escape($requirement)) {
        Write-Host "TileRenderer validation failed: $requirement" -ForegroundColor Red
        exit 1
    }
}

$saveSystemContent = Get-Content (Join-Path $projectRoot "game\State\SaveSystem.cs") -Raw
foreach ($requirement in @("class SaveSystem", "JsonSerializer", "TryLoad", "SchemaVersion")) {
    if ($saveSystemContent -notmatch [regex]::Escape($requirement)) {
        Write-Host "SaveSystem validation failed: $requirement" -ForegroundColor Red
        exit 1
    }
}

$gameWindowPath = Join-Path $projectRoot "ui\GameWindow.cs"
$gameWindowContent = Get-Content $gameWindowPath -Raw
foreach ($requirement in @("class GameWindow", "BattleSystem", "PerformBattleCommand")) {
    if ($gameWindowContent -notmatch [regex]::Escape($requirement)) {
        Write-Host "GameWindow validation failed: $requirement" -ForegroundColor Red
        exit 1
    }
}

$gameWindowLines = (Get-Content $gameWindowPath).Count
if ($gameWindowLines -gt 500) {
    Write-Host "ERROR: GameWindow.cs is over 500 lines." -ForegroundColor Red
    exit 1
}

Write-Host "Architecture checks passed." -ForegroundColor Green

dotnet build $projectFile --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
