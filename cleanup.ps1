$ErrorActionPreference = "Stop"

# ============================================================
# DOTNETCSHARP PROJECT VALIDATION
# Safe, rerunnable validation script
#
# This script:
#   - Verifies the project structure
#   - Moves accidental backup folders out of the project
#   - Removes accidental cleanup.cs
#   - Creates a backup of the current source files
#   - Validates important architecture
#   - Cleans bin/obj
#   - Builds the project
#
# IMPORTANT:
#   This script does NOT overwrite C# source files.
# ============================================================


$projectRoot =
    Split-Path -Parent $MyInvocation.MyCommand.Path

Set-Location $projectRoot


Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "DOTNETCSHARP PROJECT VALIDATION" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""


# ------------------------------------------------------------
# 1. Verify project
# ------------------------------------------------------------

$projectFile =
    Join-Path $projectRoot "DOTNETCSHARP.csproj"


if (-not (Test-Path $projectFile)) {

    Write-Host "ERROR: DOTNETCSHARP.csproj was not found." -ForegroundColor Red
    Write-Host "Current directory: $projectRoot" -ForegroundColor Red

    exit 1
}


Write-Host "Project root verified." -ForegroundColor Green


# ------------------------------------------------------------
# 2. Create backup directory OUTSIDE project
# ------------------------------------------------------------

$parentDirectory =
    Split-Path -Parent $projectRoot

$backupRoot =
    Join-Path $parentDirectory "DOTNETCSHARP_backups"

if (-not (Test-Path $backupRoot)) {

    New-Item `
        -ItemType Directory `
        -Path $backupRoot `
        -Force | Out-Null
}


$timestamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

$backupDirectory =
    Join-Path `
        $backupRoot `
        "cleanup_backup_$timestamp"


New-Item `
    -ItemType Directory `
    -Path $backupDirectory `
    -Force | Out-Null


Write-Host ""
Write-Host "Backup directory:" -ForegroundColor Gray
Write-Host "  $backupDirectory" -ForegroundColor Gray


# ------------------------------------------------------------
# 3. Move accidental backup directories OUT of project
# ------------------------------------------------------------

$oldBackupDirectories =
    Get-ChildItem `
        -Path $projectRoot `
        -Directory `
        -Filter "ui_backup_*" `
        -ErrorAction SilentlyContinue


foreach ($oldBackup in $oldBackupDirectories) {

    $destination =
        Join-Path `
            $backupRoot `
            $oldBackup.Name


    if (Test-Path $destination) {

        $destination =
            Join-Path `
                $backupRoot `
                "$($oldBackup.Name)_$timestamp"
    }


    Write-Host ""
    Write-Host "Moving old project backup:" -ForegroundColor Yellow
    Write-Host "  $($oldBackup.FullName)" -ForegroundColor Gray
    Write-Host "  -> $destination" -ForegroundColor Gray


    Move-Item `
        -Path $oldBackup.FullName `
        -Destination $destination `
        -Force
}


# ------------------------------------------------------------
# 4. Remove accidental cleanup.cs
# ------------------------------------------------------------

$oldCleanupCs =
    Join-Path `
        $projectRoot `
        "cleanup.cs"


if (Test-Path $oldCleanupCs) {

    Write-Host ""
    Write-Host "Removing accidental cleanup.cs..." -ForegroundColor Yellow


    Remove-Item `
        -Path $oldCleanupCs `
        -Force
}


# ------------------------------------------------------------
# 5. Verify required directories
# ------------------------------------------------------------

Write-Host ""
Write-Host "Checking required directories..." -ForegroundColor Cyan


$requiredDirectories = @(
    "game",
    "ui",
    "assets",
    "assets\tiles"
)


$missingDirectories = @()


foreach ($relativePath in $requiredDirectories) {

    $path =
        Join-Path `
            $projectRoot `
            $relativePath


    if (Test-Path $path) {

        Write-Host `
            "  OK  $relativePath" `
            -ForegroundColor Green
    }
    else {

        Write-Host `
            "  MISSING  $relativePath" `
            -ForegroundColor Red

        $missingDirectories += $relativePath
    }
}


if ($missingDirectories.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: Required directories are missing." -ForegroundColor Red

    foreach ($directory in $missingDirectories) {

        Write-Host `
            "  $directory" `
            -ForegroundColor Red
    }

    exit 1
}


# ------------------------------------------------------------
# 6. Verify required files
# ------------------------------------------------------------

Write-Host ""
Write-Host "Checking required files..." -ForegroundColor Cyan


# BattleSystem.cs is currently located in ui.
#
# We are deliberately validating the project as it currently
# exists rather than moving files automatically.

$requiredFiles = @(
    "Program.cs",

    "game\GameObject.cs",
    "game\GameWorld.cs",
    "game\GameLogic.cs",
    "game\DungeonGenerator.cs",
    "game\Tile.cs",
    "game\TileRenderer.cs",
    "game\GameState.cs",

    "ui\BattleSystem.cs",
    "ui\GameWindow.cs",
    "ui\GameRenderer.cs",
    "ui\ExplorationRenderer.cs",
    "ui\BattleRenderer.cs",
    "ui\StatsWindow.cs",

    "assets\tiles\dungeon.png"
)


$missingFiles = @()


foreach ($relativePath in $requiredFiles) {

    $path =
        Join-Path `
            $projectRoot `
            $relativePath


    if (Test-Path $path) {

        Write-Host `
            "  OK  $relativePath" `
            -ForegroundColor Green
    }
    else {

        Write-Host `
            "  MISSING  $relativePath" `
            -ForegroundColor Red

        $missingFiles += $relativePath
    }
}


if ($missingFiles.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: Required files are missing." -ForegroundColor Red

    foreach ($file in $missingFiles) {

        Write-Host `
            "  $file" `
            -ForegroundColor Red
    }

    exit 1
}


# ------------------------------------------------------------
# 7. Back up current source files
# ------------------------------------------------------------

Write-Host ""
Write-Host "Backing up current source files..." -ForegroundColor Cyan


$filesToBackup = @(
    "Program.cs",

    "game\GameObject.cs",
    "game\GameWorld.cs",
    "game\GameLogic.cs",
    "game\DungeonGenerator.cs",
    "game\Tile.cs",
    "game\TileRenderer.cs",
    "game\GameState.cs",

    "ui\BattleSystem.cs",
    "ui\GameWindow.cs",
    "ui\GameRenderer.cs",
    "ui\ExplorationRenderer.cs",
    "ui\BattleRenderer.cs",
    "ui\StatsWindow.cs",

    "DOTNETCSHARP.csproj"
)


foreach ($relativePath in $filesToBackup) {

    $source =
        Join-Path `
            $projectRoot `
            $relativePath


    if (-not (Test-Path $source)) {
        continue
    }


    $destination =
        Join-Path `
            $backupDirectory `
            $relativePath


    $destinationDirectory =
        Split-Path `
            -Parent `
            $destination


    if (-not (Test-Path $destinationDirectory)) {

        New-Item `
            -ItemType Directory `
            -Path $destinationDirectory `
            -Force | Out-Null
    }


    Copy-Item `
        -Path $source `
        -Destination $destination `
        -Force


    Write-Host `
        "  Backed up $relativePath" `
        -ForegroundColor Gray
}


# ------------------------------------------------------------
# 8. Validate BattleSystem
# ------------------------------------------------------------

Write-Host ""
Write-Host "Validating BattleSystem..." -ForegroundColor Cyan


$battleSystemPath =
    Join-Path `
        $projectRoot `
        "ui\BattleSystem.cs"


$battleSystemContent =
    Get-Content `
        $battleSystemPath `
        -Raw


$battleSystemRequirements = @(
    "class BattleSystem",
    "enum BattleResult",
    "BattleCommand",
    "PerformPlayerTurn",
    "PlayerAttack",
    "EnemyAttack"
)


$battleSystemErrors = @()


foreach ($requirement in $battleSystemRequirements) {

    if ($battleSystemContent -notmatch [regex]::Escape($requirement)) {

        $battleSystemErrors += $requirement
    }
}


if ($battleSystemErrors.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: BattleSystem validation failed." -ForegroundColor Red

    foreach ($requirement in $battleSystemErrors) {

        Write-Host `
            "  Missing: $requirement" `
            -ForegroundColor Red
    }

    exit 1
}


Write-Host "  BattleSystem structure OK." -ForegroundColor Green


# ------------------------------------------------------------
# 9. Validate GameRenderer
# ------------------------------------------------------------

Write-Host ""
Write-Host "Validating GameRenderer..." -ForegroundColor Cyan


$gameRendererPath =
    Join-Path `
        $projectRoot `
        "ui\GameRenderer.cs"


$gameRendererContent =
    Get-Content `
        $gameRendererPath `
        -Raw


$gameRendererRequirements = @(
    "class GameRenderer",
    "ExplorationRenderer",
    "BattleRenderer",
    "BattleCommand",
    "selectedCommand"
)


$gameRendererErrors = @()


foreach ($requirement in $gameRendererRequirements) {

    if ($gameRendererContent -notmatch [regex]::Escape($requirement)) {

        $gameRendererErrors += $requirement
    }
}


if ($gameRendererErrors.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: GameRenderer validation failed." -ForegroundColor Red

    foreach ($requirement in $gameRendererErrors) {

        Write-Host `
            "  Missing: $requirement" `
            -ForegroundColor Red
    }

    exit 1
}


Write-Host "  GameRenderer structure OK." -ForegroundColor Green


# ------------------------------------------------------------
# 10. Validate BattleRenderer
# ------------------------------------------------------------

Write-Host ""
Write-Host "Validating BattleRenderer..." -ForegroundColor Cyan


$battleRendererPath =
    Join-Path `
        $projectRoot `
        "ui\BattleRenderer.cs"


$battleRendererContent =
    Get-Content `
        $battleRendererPath `
        -Raw


$battleRendererRequirements = @(
    "class BattleRenderer",
    "BattleCommand",
    "DrawCommands",
    "DrawMessage",
    "RectangleF"
)


$battleRendererErrors = @()


foreach ($requirement in $battleRendererRequirements) {

    if ($battleRendererContent -notmatch [regex]::Escape($requirement)) {

        $battleRendererErrors += $requirement
    }
}


if ($battleRendererErrors.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: BattleRenderer validation failed." -ForegroundColor Red

    foreach ($requirement in $battleRendererErrors) {

        Write-Host `
            "  Missing: $requirement" `
            -ForegroundColor Red
    }

    exit 1
}


Write-Host "  BattleRenderer structure OK." -ForegroundColor Green


# ------------------------------------------------------------
# 11. Validate GameWindow
# ------------------------------------------------------------

Write-Host ""
Write-Host "Validating GameWindow..." -ForegroundColor Cyan


$gameWindowPath =
    Join-Path `
        $projectRoot `
        "ui\GameWindow.cs"


$gameWindowContent =
    Get-Content `
        $gameWindowPath `
        -Raw


$gameWindowRequirements = @(
    "class GameWindow",
    "BattleSystem",
    "battleSystem",
    "PerformBattleCommand",
    "selectedCommand"
)


$gameWindowErrors = @()


foreach ($requirement in $gameWindowRequirements) {

    if ($gameWindowContent -notmatch [regex]::Escape($requirement)) {

        $gameWindowErrors += $requirement
    }
}


if ($gameWindowErrors.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: GameWindow validation failed." -ForegroundColor Red

    foreach ($requirement in $gameWindowErrors) {

        Write-Host `
            "  Missing: $requirement" `
            -ForegroundColor Red
    }

    exit 1
}


$gameWindowLines =
    (Get-Content $gameWindowPath).Count


Write-Host `
    "  GameWindow.cs line count: $gameWindowLines" `
    -ForegroundColor Gray


if ($gameWindowLines -gt 500) {

    Write-Host ""
    Write-Host "ERROR: GameWindow.cs is over 500 lines." -ForegroundColor Red

    exit 1
}


Write-Host "  GameWindow structure OK." -ForegroundColor Green


# ------------------------------------------------------------
# 12. Ensure no backup source files remain in project
# ------------------------------------------------------------

Write-Host ""
Write-Host "Checking for backup source files inside project..." -ForegroundColor Cyan


$remainingBackupSources =
    Get-ChildItem `
        -Path $projectRoot `
        -Recurse `
        -File `
        -Include *.cs `
        -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -match "\\ui_backup_"
    }


if ($remainingBackupSources.Count -gt 0) {

    Write-Host ""
    Write-Host "ERROR: Backup .cs files still exist inside project." -ForegroundColor Red

    foreach ($file in $remainingBackupSources) {

        Write-Host `
            "  $($file.FullName)" `
            -ForegroundColor Red
    }

    exit 1
}


Write-Host `
    "No backup source files found inside project." `
    -ForegroundColor Green


# ------------------------------------------------------------
# 13. Clean build artifacts
# ------------------------------------------------------------

Write-Host ""
Write-Host "Cleaning bin and obj..." -ForegroundColor Cyan


$binDirectory =
    Join-Path `
        $projectRoot `
        "bin"


$objDirectory =
    Join-Path `
        $projectRoot `
        "obj"


if (Test-Path $binDirectory) {

    Remove-Item `
        -Path $binDirectory `
        -Recurse `
        -Force
}


if (Test-Path $objDirectory) {

    Remove-Item `
        -Path $objDirectory `
        -Recurse `
        -Force
}


Write-Host `
    "Build artifacts removed." `
    -ForegroundColor Green


# ------------------------------------------------------------
# 14. Build
# ------------------------------------------------------------

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "BUILDING PROJECT" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""


dotnet build $projectFile


if ($LASTEXITCODE -ne 0) {

    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host "BUILD FAILED" -ForegroundColor Red
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host ""

    Write-Host `
        "The project did not compile." `
        -ForegroundColor Red

    Write-Host `
        "No C# source files were modified by this script." `
        -ForegroundColor Yellow

    Write-Host ""
    Write-Host "Backup available at:" -ForegroundColor Yellow
    Write-Host "  $backupDirectory" -ForegroundColor Gray

    exit 1
}


# ------------------------------------------------------------
# 15. Success
# ------------------------------------------------------------

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""


Write-Host `
    "Project validation completed successfully." `
    -ForegroundColor Green


Write-Host ""
Write-Host "Backup:" -ForegroundColor Gray
Write-Host "  $backupDirectory" -ForegroundColor Gray


Write-Host ""
Write-Host "The game was NOT launched automatically." -ForegroundColor Cyan


Write-Host ""
Write-Host "Next command:" -ForegroundColor Cyan
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
