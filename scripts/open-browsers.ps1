<#
.SYNOPSIS
    Ouvre l'écran TV, l'interface GM et plusieurs joueurs dans des fenêtres de navigation privée.

.DESCRIPTION
    Pour tester l'application sur un seul PC. Chaque fenêtre utilise son propre profil temporaire
    en plus du mode privé : les fenêtres privées d'un même profil partagent leur localStorage, et
    tous les joueurs se retrouveraient sinon avec le même jeton.

    Le serveur doit déjà tourner (.\scripts\start.ps1 ou npm run dev).

.PARAMETER Players
    Nombre de fenêtres joueur à ouvrir (0 à 20).

.PARAMETER BaseUrl
    Adresse du serveur. Par défaut : http://localhost:5000 (ou le port passé par -Port).
    Utiliser http://localhost:5173 pour le serveur de développement Vite.

.PARAMETER Port
    Port du serveur .NET, si BaseUrl n'est pas précisé.

.PARAMETER Browser
    Navigateur à utiliser : chrome ou edge. Par défaut : Chrome s'il est installé, sinon Edge.

.EXAMPLE
    .\scripts\open-browsers.ps1 3
    .\scripts\open-browsers.ps1 -Players 4 -BaseUrl http://localhost:5173 -Browser edge
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateRange(0, 20)]
    [int] $Players = 3,

    [string] $BaseUrl,

    [ValidateRange(1, 65535)]
    [int] $Port = 5000,

    [ValidateSet('chrome', 'edge')]
    [string] $Browser
)

$ErrorActionPreference = 'Stop'

function Find-Browser([string] $name) {
    $candidates = switch ($name) {
        'chrome' {
            "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
            "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
            "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
        }
        'edge' {
            "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
            "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
        }
    }
    return $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}

if ($Browser) {
    $exe = Find-Browser $Browser
    if (-not $exe) {
        Write-Host "Navigateur introuvable : $Browser" -ForegroundColor Red
        exit 1
    }
}
else {
    $Browser = 'chrome'
    $exe = Find-Browser 'chrome'
    if (-not $exe) {
        $Browser = 'edge'
        $exe = Find-Browser 'edge'
    }
    if (-not $exe) {
        Write-Host 'Ni Chrome ni Edge ne sont installés.' -ForegroundColor Red
        exit 1
    }
}
$privateFlag = if ($Browser -eq 'edge') { '--inprivate' } else { '--incognito' }

if (-not $BaseUrl) {
    $BaseUrl = "http://localhost:$Port"
}
$BaseUrl = $BaseUrl.TrimEnd('/')

try {
    Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 3 | Out-Null
}
catch {
    Write-Host "    Attention : $BaseUrl ne répond pas. Lancez d'abord le serveur (.\scripts\start.ps1)." -ForegroundColor Yellow
}

$profilesRoot = Join-Path $env:TEMP 'PartyGame-browsers'

Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea

function Open-Window([string] $name, [string] $url, [int] $x, [int] $y, [int] $width, [int] $height) {
    $profileDir = Join-Path $profilesRoot $name
    # A fresh profile each run, so a player never inherits a token from a previous game. Removal
    # fails while a window from a previous run still uses the profile: it is then simply reused.
    if (Test-Path $profileDir) {
        Remove-Item -Recurse -Force $profileDir -ErrorAction SilentlyContinue
    }
    $arguments = @(
        $privateFlag
        "--user-data-dir=`"$profileDir`""
        '--no-first-run'
        '--no-default-browser-check'
        '--new-window'
        "--window-position=$x,$y"
        "--window-size=$width,$height"
        $url
    )
    Start-Process -FilePath $exe -ArgumentList $arguments | Out-Null
    Write-Host "    $name -> $url"
}

Write-Host "==> Ouverture dans $Browser ($BaseUrl)" -ForegroundColor Cyan

# Display and GM share the top half of the screen; players are tiled along the bottom half,
# overlapping when there are too many to fit side by side.
$halfWidth = [int] ($screen.Width / 2)
$topHeight = [int] ($screen.Height / 2)
Open-Window 'display' "$BaseUrl/display/" $screen.X $screen.Y $halfWidth $topHeight
Open-Window 'gm' "$BaseUrl/gm/" ($screen.X + $halfWidth) $screen.Y $halfWidth $topHeight

if ($Players -gt 0) {
    $playerWidth = 420
    $playerHeight = [Math]::Max(500, $screen.Height - $topHeight)
    $playerY = $screen.Y + $screen.Height - $playerHeight
    $step = if ($Players -gt 1) { [Math]::Min($playerWidth, [int] (($screen.Width - $playerWidth) / ($Players - 1))) } else { 0 }
    for ($i = 1; $i -le $Players; $i++) {
        Open-Window "player-$i" "$BaseUrl/" ($screen.X + ($i - 1) * $step) $playerY $playerWidth $playerHeight
    }
}
