<#
.SYNOPSIS
    Lance le serveur PartyGame après avoir vérifié que tout est en ordre.

.DESCRIPTION
    Vérifie les prérequis (SDK .NET, Node.js), installe les dépendances du front si elles manquent
    ou ont changé, contrôle que les types TypeScript générés correspondent à PartyGame.Contracts,
    reconstruit le front s'il n'est plus à jour, signale un port occupé ou un réseau Windows
    déclaré public, puis lance le serveur avec les packs du dossier packs/ du dépôt (sauf si la
    variable d'environnement Packs__Directory en désigne un autre).

.PARAMETER Port
    Port d'écoute. Par défaut : la variable d'environnement Network__Port, sinon 5000.

.PARAMETER GameMasterCode
    Code GM imposé (6 chiffres), pour le développement. À ne pas utiliser pour une vraie soirée.

.PARAMETER Rebuild
    Reconstruit le front même s'il semble à jour.

.EXAMPLE
    .\scripts\start.ps1
    .\scripts\start.ps1 -Port 5001 -GameMasterCode 123456
#>
[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int] $Port = 0,

    [ValidatePattern('^\d{6}$')]
    [string] $GameMasterCode,

    [switch] $Rebuild
)

$ErrorActionPreference = 'Stop'
# French messages, and the output of npm and of the server, are UTF-8 whatever the console code page.
$previousEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root = Split-Path -Parent $PSScriptRoot
$client = Join-Path $root 'client'
$serverProject = Join-Path $root 'src\PartyGame.Server'
$contractsProject = Join-Path $root 'src\PartyGame.Contracts'
$webRoot = Join-Path $serverProject 'wwwroot'
# Lives in the build output: any other build (npm run build, E2E tests) replaces it, so a stamp
# can never vouch for a build it did not produce.
$buildStamp = Join-Path $webRoot '.build-stamp'

function Write-Step([string] $message) {
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Write-Warn([string] $message) {
    Write-Host "    Attention : $message" -ForegroundColor Yellow
}

function Stop-Launch([string] $message) {
    Write-Host ''
    Write-Host "Lancement impossible : $message" -ForegroundColor Red
    exit 1
}

function Invoke-Native([string] $what, [scriptblock] $command) {
    & $command
    if ($LASTEXITCODE -ne 0) {
        Stop-Launch "$what a échoué (code $LASTEXITCODE)."
    }
}

# Hash of every file the front build depends on, plus the contracts the generated types come from:
# when it has not changed since the last build, the build is up to date.
function Get-FrontInputsHash {
    $files = @(
        Get-ChildItem -Path (Join-Path $client 'src'), (Join-Path $client 'vite') -Recurse -File |
            Where-Object { $_.Name -notlike '*.test.ts' }
        Get-ChildItem -Path $contractsProject -Recurse -File -Filter '*.cs' |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
        foreach ($name in 'index.html', 'display\index.html', 'gm\index.html', 'vite.config.ts',
            'svelte.config.js', 'tsconfig.json', 'package.json', 'package-lock.json') {
            Get-Item -Path (Join-Path $client $name)
        }
    )
    $lines = $files |
        Sort-Object FullName |
        ForEach-Object { '{0}:{1}' -f $_.FullName.Substring($root.Length), (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

Push-Location $root
try {
    # --- Prerequisites ------------------------------------------------------------------------
    Write-Step 'Vérification des prérequis'
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Stop-Launch 'le SDK .NET est introuvable. Installez le SDK .NET 10 : https://dotnet.microsoft.com/download'
    }
    # Run from the repository root, `dotnet --version` fails when global.json asks for a missing SDK.
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Stop-Launch 'aucun SDK .NET installé ne correspond à global.json. Installez le SDK .NET 10.'
    }
    if (-not (Get-Command node -ErrorAction SilentlyContinue) -or -not (Get-Command npm -ErrorAction SilentlyContinue)) {
        Stop-Launch 'Node.js est introuvable. Installez Node.js 22 LTS ou plus récent : https://nodejs.org'
    }
    $nodeVersion = (& node --version).TrimStart('v')
    if ([version] $nodeVersion -lt [version] '20.19.0') {
        Stop-Launch "Node.js $nodeVersion est trop ancien pour Vite. Installez Node.js 22 LTS ou plus récent."
    }
    Write-Host "    SDK .NET $dotnetVersion, Node.js $nodeVersion"

    # --- Front dependencies -------------------------------------------------------------------
    Write-Step 'Dépendances du front'
    $installedLock = Join-Path $client 'node_modules\.package-lock.json'
    $lock = Join-Path $client 'package-lock.json'
    if (-not (Test-Path $installedLock) -or (Get-Item $lock).LastWriteTimeUtc -gt (Get-Item $installedLock).LastWriteTimeUtc) {
        Write-Host '    Installation (npm ci)…'
        Push-Location $client
        try {
            Invoke-Native 'npm ci' { npm ci --no-audit --no-fund }
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Host '    À jour'
    }

    # --- Front build --------------------------------------------------------------------------
    Write-Step 'Build du front'
    $hash = Get-FrontInputsHash
    $upToDate = -not $Rebuild `
        -and (Test-Path (Join-Path $webRoot 'index.html')) `
        -and (Test-Path $buildStamp) `
        -and ((Get-Content $buildStamp -Raw).Trim() -eq $hash)
    if ($upToDate) {
        Write-Host '    À jour'
    }
    else {
        Write-Host '    Vérification des types générés depuis PartyGame.Contracts…'
        Push-Location $client
        try {
            & node scripts/checkContracts.js
            if ($LASTEXITCODE -ne 0) {
                Stop-Launch 'les types TypeScript générés ne correspondent plus à PartyGame.Contracts. Lancez « npm run generate:contracts » dans client/, puis relancez ce script.'
            }
            Write-Host '    Construction (npm run build)…'
            Invoke-Native 'npm run build' { npm run build }
        }
        finally {
            Pop-Location
        }
        Set-Content -Path $buildStamp -Value $hash -Encoding ASCII
    }

    # --- Network ------------------------------------------------------------------------------
    Write-Step 'Réseau'
    if ($Port -eq 0) {
        $Port = if ($env:Network__Port) { [int] $env:Network__Port } else { 5000 }
    }
    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener) {
        $owner = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
        $ownerName = if ($owner) { "$($owner.ProcessName) (PID $($owner.Id))" } else { "PID $($listener.OwningProcess)" }
        Stop-Launch "le port $Port est déjà utilisé par $ownerName. Arrêtez ce programme ou choisissez un autre port : -Port 5001."
    }
    $publicNetworks = @(Get-NetConnectionProfile -ErrorAction SilentlyContinue | Where-Object { $_.NetworkCategory -eq 'Public' })
    foreach ($network in $publicNetworks) {
        Write-Warn "le réseau « $($network.Name) » ($($network.InterfaceAlias)) est déclaré public : Windows y bloque les téléphones. Déclarez-le privé (voir docs/installation.md)."
    }
    Write-Host "    Port $Port libre"

    # --- Server -------------------------------------------------------------------------------
    Write-Step 'Lancement du serveur (Ctrl+C pour l''arrêter)'
    $serverArgs = @("--Network:Port=$Port")
    # The packs of the repository, unless the environment points to other ones. Relative to the
    # server executable otherwise, the default would find no pack under `dotnet run`.
    if (-not $env:Packs__Directory) {
        $serverArgs += "--Packs:Directory=$(Join-Path $root 'packs')"
    }
    if ($GameMasterCode) {
        $serverArgs += "--GameMaster:Code=$GameMasterCode"
    }
    & dotnet run --project $serverProject -- @serverArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
    [Console]::OutputEncoding = $previousEncoding
}
