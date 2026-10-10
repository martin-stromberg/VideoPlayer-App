#Requires -Version 7.2
<#
.SYNOPSIS
    Setzt die iOS-Signatur-Secrets fuer die GitHub-Actions-Pipeline (package-ios).

.DESCRIPTION
    Die Workflows staging-merge.yml und main-merge.yml laden die signierte .ipa
    nach TestFlight, sobald die Repository-Variable IOS_SIGNING_ENABLED=true und
    die Secrets IOS_* gesetzt sind (Vorlage: Repository martin-stromberg/Reporter).

    GitHub-Secret-Werte lassen sich nicht zuruecklesen - das Skript sammelt das
    Material deshalb aus den lokalen Quellen:

      - VIDEOPLAYER_IOS_*-/REPORTER_IOS_*-Umgebungsvariablen (User-Scope), wie sie
        scripts/iOS-Deployment.ps1 bzw. das Reporter-Pendant verwenden
      - Signaturdateien (.p12/.p8/.mobileprovision) unter -P12Path, -ApiKeyPath,
        -ProvisioningProfilePath oder den bekannten Ablageorten

    Gesetzt werden:
      IOS_CODESIGN_KEY                 <- VIDEOPLAYER_/REPORTER_IOS_CODESIGN_KEY
      IOS_PROVISIONING_PROFILE         <- VIDEOPLAYER_IOS_PROVISIONING_PROFILE
      IOS_PROVISIONING_PROFILE_BASE64  <- .mobileprovision (base64)
      IOS_API_KEY_ID                   <- VIDEOPLAYER_/REPORTER_IOS_API_KEY_ID
      IOS_API_ISSUER_ID                <- VIDEOPLAYER_/REPORTER_IOS_API_ISSUER_ID
      IOS_API_KEY_P8                   <- Inhalt der .p8-Datei (-ApiKeyPath)
      IOS_CERTIFICATE_P12_BASE64       <- .p12 (base64, -P12Path)
      IOS_CERTIFICATE_P12_PASSWORD     <- -P12Password (Export-Passwort)
      Variable IOS_SIGNING_ENABLED=true

.EXAMPLE
    pwsh scripts/Set-IosSigningSecrets.ps1 -P12Password '<export-passwort>'

.EXAMPLE
    pwsh scripts/Set-IosSigningSecrets.ps1 -P12Password '<pw>' -DryRun
#>
param(
    # Repository im Format owner/name.
    [string]$Repository = 'martin-stromberg/VideoPlayer-App',

    # Passwort des .p12-Exports. Alternativ VIDEOPLAYER_IOS_P12_PASSWORD.
    # Einziges Material, das lokal nicht ableitbar ist.
    [string]$P12Password = [Environment]::GetEnvironmentVariable('VIDEOPLAYER_IOS_P12_PASSWORD', 'User'),

    # Distribution-Zertifikat-Export (.p12, Zertifikat + privater Schluessel).
    # Standard: neueste Datei "Apple Distribution*.p12" unter Y:\Development
    # oder ~\Downloads.
    [string]$P12Path,

    # App-Store-Provisioning-Profil fuer de.martinstromberg.videos.
    # Standard: VideoPlayer_AppStore.mobileprovision unter Y:\Development
    # oder ~\Downloads.
    [string]$ProvisioningProfilePath,

    # App-Store-Connect-API-Schluessel (.p8).
    # Standard: VIDEOPLAYER_/REPORTER_IOS_API_KEY_PATH.
    [string]$ApiKeyPath,

    # Nur anzeigen, was gesetzt wuerde - kein Schreibzugriff auf GitHub.
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Get-UserEnv([string[]]$Names) {
    foreach ($name in $Names) {
        $value = [Environment]::GetEnvironmentVariable($name, 'User')
        if (-not $value) { $value = [Environment]::GetEnvironmentVariable($name, 'Process') }
        if ($value) { return $value }
    }
    return $null
}

function Resolve-FirstExisting([string[]]$Candidates) {
    foreach ($candidate in $Candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    return $null
}

function Find-LatestFile([string[]]$Directories, [string]$Filter) {
    $files = foreach ($dir in $Directories) {
        if ($dir -and (Test-Path -LiteralPath $dir)) {
            Get-ChildItem -LiteralPath $dir -Filter $Filter -File -ErrorAction SilentlyContinue
        }
    }
    $files | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}

$searchDirs = @('Y:\Development', (Join-Path $env:USERPROFILE 'Downloads'))

$codesignKey = Get-UserEnv 'VIDEOPLAYER_IOS_CODESIGN_KEY', 'REPORTER_IOS_CODESIGN_KEY'
$profileName = Get-UserEnv 'VIDEOPLAYER_IOS_PROVISIONING_PROFILE'
$apiKeyId = Get-UserEnv 'VIDEOPLAYER_IOS_API_KEY_ID', 'REPORTER_IOS_API_KEY_ID'
$apiIssuerId = Get-UserEnv 'VIDEOPLAYER_IOS_API_ISSUER_ID', 'REPORTER_IOS_API_ISSUER_ID'

if (-not $ApiKeyPath) {
    $ApiKeyPath = Get-UserEnv 'VIDEOPLAYER_IOS_API_KEY_PATH', 'REPORTER_IOS_API_KEY_PATH'
}
$ApiKeyPath = Resolve-FirstExisting @($ApiKeyPath)
if (-not $ProvisioningProfilePath) {
    $ProvisioningProfilePath = Find-LatestFile $searchDirs 'VideoPlayer_AppStore.mobileprovision'
}
$ProvisioningProfilePath = Resolve-FirstExisting @($ProvisioningProfilePath)
if (-not $P12Path) {
    $P12Path = Find-LatestFile $searchDirs 'Apple Distribution*.p12'
}
$P12Path = Resolve-FirstExisting @($P12Path)

$secrets = [ordered]@{
    IOS_CODESIGN_KEY                 = $codesignKey
    IOS_PROVISIONING_PROFILE         = $profileName
    IOS_API_KEY_ID                   = $apiKeyId
    IOS_API_ISSUER_ID                = $apiIssuerId
    IOS_API_KEY_P8                   = if ($ApiKeyPath) { (Get-Content -LiteralPath $ApiKeyPath -Raw).Trim() }
    IOS_CERTIFICATE_P12_BASE64       = if ($P12Path) { [Convert]::ToBase64String([IO.File]::ReadAllBytes($P12Path)) }
    IOS_CERTIFICATE_P12_PASSWORD     = $P12Password
    IOS_PROVISIONING_PROFILE_BASE64  = if ($ProvisioningProfilePath) { [Convert]::ToBase64String([IO.File]::ReadAllBytes($ProvisioningProfilePath)) }
}

Write-Host "Quelldateien:"
Write-Host "  .p8  API-Key        : $($ApiKeyPath ?? 'nicht gefunden')"
Write-Host "  .p12 Zertifikat     : $($P12Path ?? 'nicht gefunden')"
Write-Host "  .mobileprovision    : $($ProvisioningProfilePath ?? 'nicht gefunden')"
Write-Host ""

$missing = @()
foreach ($entry in $secrets.GetEnumerator()) {
    $status = if ($entry.Value) { "gesetzt ($($entry.Value.Length) Zeichen)" } else { $missing += $entry.Key; 'FEHLT' }
    Write-Host ("  {0,-35} {1}" -f $entry.Key, $status)
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Warning "Fehlende Werte: $($missing -join ', ')"
    Write-Warning 'Das .p12-Passwort ist nicht ableitbar - per -P12Password uebergeben.'
}

if ($DryRun) {
    Write-Host ""
    Write-Host 'DryRun: es wird nichts nach GitHub geschrieben.'
    return
}
if ($missing.Count -gt 0) {
    throw 'Abbruch: nicht alle Secrets konnten aufgeloest werden (siehe oben).'
}

foreach ($entry in $secrets.GetEnumerator()) {
    $entry.Value | gh secret set $entry.Key --repo $Repository
    if ($LASTEXITCODE -ne 0) { throw "gh secret set $($entry.Key) fehlgeschlagen (Exit $LASTEXITCODE)." }
    Write-Host "  gesetzt: $($entry.Key)"
}

gh variable set IOS_SIGNING_ENABLED --body 'true' --repo $Repository
if ($LASTEXITCODE -ne 0) { throw "gh variable set IOS_SIGNING_ENABLED fehlgeschlagen (Exit $LASTEXITCODE)." }
Write-Host '  gesetzt: Variable IOS_SIGNING_ENABLED=true'

Write-Host ""
Write-Host "Fertig. Der package-ios-Job in staging-merge.yml/main-merge.yml laeuft ab dem naechsten Push."
