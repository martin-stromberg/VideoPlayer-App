<#
    tvOS-Deployment.ps1

    Build und Deployment des tvOS-Teils von VideoPlayer (VideoPlayer.TvOS).
    (Muster: scripts/iOS-Deployment.ps1)

    Moegliche Aktionen:
        build     -> tvOS-App bauen (mit Codesigning: .ipa)
        simulator -> App bauen, im tvOS-Simulator starten und Screenshot speichern
        device    -> App bauen und auf einem echten Apple TV starten
        store     -> Signierten Release-Build erzeugen, validieren und zu App Store Connect
                     hochladen (TestFlight). Setzt die Buildnummer (CFBundleVersion) auf die
                     Anzahl der Commits - dieselbe Zaehlung wie die CI-Pipeline
                     (.github/actions/package-tvos) (Opt-out: -NoBumpBuildNumber,
                     manueller Override: -BuildNumber).
        upload    -> Vorhandene .ipa validieren und zu App Store Connect hochladen
        list      -> Simulatoren/Geraete, Signaturidentitaeten und Profile auf dem Mac anzeigen
        menu      -> Interaktives Menue

    Plattform:
        VideoPlayer.TvOS ist KEIN MAUI-Projekt - das Pair-to-Mac-MSBuild-
        Remoting (ServerAddress/TcpPort/_DotNetRootRemoteDirectory) greift
        nicht. Die tvOS-Workload ist nur auf macOS installierbar:
        - Auf macOS laufen alle Aktionen direkt im lokalen Repo-Checkout.
        - Auf Windows wird per schluesselbasiertem SSH (BatchMode, kein
          Passwort-Prompt) auf einen Mac delegiert, der einen eigenen
          Repo-Checkout unter -RemoteRepoPath (Umgebungsvariable
          VIDEOPLAYER_TVOS_MAC_REPO_PATH, Standard ~/VideoPlayer-App)
          bereithaelt. Build-Aktionen synchronisieren den Remote-Checkout
          vorab auf den lokalen HEAD-Commit (git fetch + checkout).
        -Action upload akzeptiert unter Windows eine lokale .ipa
        (-IpaPath), die per scp auf den Mac kopiert und von dort
        hochgeladen wird.

    Umgebungsvariablen:
        VIDEOPLAYER_TVOS_CODESIGN_KEY
        VIDEOPLAYER_TVOS_PROVISIONING_PROFILE
        VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS
        VIDEOPLAYER_TVOS_MAC_SERVER_USER
        VIDEOPLAYER_TVOS_MAC_REPO_PATH     -> Repo-Checkout auf dem Mac (Standard: ~/VideoPlayer-App)
        VIDEOPLAYER_TVOS_API_KEY_PATH      -> Pfad zum App Store Connect API Key (.p8, ausserhalb des Repo)
        VIDEOPLAYER_TVOS_API_KEY_ID        -> Key-ID aus App Store Connect
        VIDEOPLAYER_TVOS_API_ISSUER_ID     -> Issuer-ID aus App Store Connect
#>

param(
    [ValidateSet("build", "simulator", "device", "store", "upload", "list", "menu")]
    [string]$Action = "menu",

    [Parameter(HelpMessage = "UDID oder Name des Simulators / Geraets.")]
    [string]$Device = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "",

    [Parameter(HelpMessage = "tvossimulator-arm64 | tvossimulator-x64 | tvos-arm64")]
    [string]$RuntimeIdentifier = "",

    [string]$ServerAddress = $env:VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS,
    [Parameter(HelpMessage = "Der macOS-Kurzname (z. B. martin), nicht der volle Benutzername.")]
    [string]$ServerUser = $env:VIDEOPLAYER_TVOS_MAC_SERVER_USER,
    [Parameter(HelpMessage = "Pfad des Repo-Checkouts auf dem Mac (Remote-Builds, Remote-Uploads).")]
    [string]$RemoteRepoPath = $env:VIDEOPLAYER_TVOS_MAC_REPO_PATH,

    [string]$CodesignKey = $env:VIDEOPLAYER_TVOS_CODESIGN_KEY,
    [string]$CodesignProvision = $env:VIDEOPLAYER_TVOS_PROVISIONING_PROFILE,
    [string]$CodesignEntitlements = "",

    [Parameter(HelpMessage = "Pfad zum App Store Connect API Key (.p8). Muss ausserhalb des Repository liegen.")]
    [string]$ApiKeyPath = $env:VIDEOPLAYER_TVOS_API_KEY_PATH,
    [string]$ApiKeyId = $env:VIDEOPLAYER_TVOS_API_KEY_ID,
    [string]$ApiIssuerId = $env:VIDEOPLAYER_TVOS_API_ISSUER_ID,
    [Parameter(HelpMessage = "Pfad zu einer vorhandenen .ipa (nur Aktion 'upload').")]
    [string]$IpaPath = "",
    [Parameter(HelpMessage = "Bei 'device': App-Output (--console) nach dem Start streamen (Ctrl+C zum Loesen).")]
    [switch]$Console,
    [switch]$NoBumpBuildNumber,
    [Parameter(HelpMessage = "CFBundleVersion manuell setzen (Standard: Anzahl der Commits, wie CI). Fuer einen erneuten Upload desselben Commits einen groesseren Wert uebergeben.")]
    [int]$BuildNumber = 0,
    [string]$LogDir = "logs",

    [switch]$NoPrompt
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = "VideoPlayer.TvOS/VideoPlayer.TvOS.csproj"
$framework = "net10.0-tvos"

# Manifest-Pin deckungsgleich mit dem build-tvos-Job (pr-checks.yml)
# und dem package-tvos-Default (.github/actions/package-tvos).
$workloadVersion = "26.5.10318"

$isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$isMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)

if (-not $RemoteRepoPath) { $RemoteRepoPath = '$HOME/VideoPlayer-App' }

function Get-RuntimeIdentifier {
    param([string]$Action)

    if ($RuntimeIdentifier) { return $RuntimeIdentifier }
    if ($Action -eq "simulator") {
        return "tvossimulator-arm64"
    }
    return "tvos-arm64"
}

function Get-Configuration {
    param([string]$Action)
    if ($Configuration) { return $Configuration }
    if ($Action -eq "simulator") { return "Debug" }
    return "Release"
}

function Assert-MacTarget {
    # Alle tvOS-Aktionen brauchen einen macOS-Host: lokal oder per SSH.
    if ($isMacOS) { return }
    if (-not ($ServerAddress -and $ServerUser)) {
        Write-Host "Fehler: tvOS-Builds sind nur auf macOS moeglich. Auf Windows wird ein SSH-Zugang" -ForegroundColor Red
        Write-Host "zu einem Mac mit eigenem Repo-Checkout benoetigt." -ForegroundColor Red
        Write-Host "Setze -ServerAddress / -ServerUser bzw.:" -ForegroundColor Yellow
        Write-Host "VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS, VIDEOPLAYER_TVOS_MAC_SERVER_USER" -ForegroundColor Yellow
        Write-Host "Voraussetzung: schluesselbasiertes SSH (kein Passwort-Prompt) und ein" -ForegroundColor Yellow
        Write-Host "Repo-Checkout unter $RemoteRepoPath (anpassbar via -RemoteRepoPath /" -ForegroundColor Yellow
        Write-Host "VIDEOPLAYER_TVOS_MAC_REPO_PATH)." -ForegroundColor Yellow
        exit 1
    }
}

function Invoke-MacCapture {
    # Fuehrt ein Bash-Skript lokal (macOS) oder per SSH aus und gibt die stdout-Zeilen zurueck.
    param([string]$Script)
    # CRLF aus den Here-Strings wuerde das Remote-Bash brechen
    # (set -e\r, trap EXIT\r, \r in Pfaden) - auf LF normalisieren.
    $b64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($Script -replace "`r`n", "`n")))
    if ($isMacOS) {
        return @(& /bin/bash -c "echo $b64 | base64 -d | bash")
    }
    return @(& ssh -o BatchMode=yes -o ConnectTimeout=15 "$ServerUser@$ServerAddress" "echo $b64 | base64 -d | bash")
}

function Invoke-OnMac {
    param(
        [string]$Script,
        [string]$Description,
        [switch]$ReturnExitCode
    )
    $b64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($Script -replace "`r`n", "`n")))
    if ($isMacOS) {
        & /bin/bash -c "echo $b64 | base64 -d | bash"
        $exitCode = $LASTEXITCODE
    }
    else {
        & ssh -o BatchMode=yes -o ConnectTimeout=10 "$ServerUser@$ServerAddress" "echo $b64 | base64 -d | bash"
        $exitCode = $LASTEXITCODE
    }
    if ($ReturnExitCode) { return $exitCode }
    if ($exitCode -ne 0) {
        Write-Host "Fehler: $Description fehlgeschlagen (Exit-Code: $exitCode)." -ForegroundColor Red
        if (-not $isMacOS) {
            Write-Host "SSH-Ausfuehrung auf $ServerUser@$ServerAddress nicht moeglich oder Befehl fehlgeschlagen." -ForegroundColor Yellow
            Write-Host "Fuehre auf dem Mac manuell aus:" -ForegroundColor Yellow
            Write-Host $Script -ForegroundColor Gray
        }
        exit 1
    }
}

function Get-RemoteHome {
    if ($script:RemoteHome) { return $script:RemoteHome }
    $out = & ssh -o BatchMode=yes -o ConnectTimeout=10 "$ServerUser@$ServerAddress" 'printf %s "$HOME"'
    if ($LASTEXITCODE -ne 0 -or -not $out) {
        Write-Host "Fehler: Remote-Home-Verzeichnis auf ${ServerUser}@${ServerAddress} konnte nicht ermittelt werden." -ForegroundColor Red
        exit 1
    }
    $script:RemoteHome = $out.Trim()
    return $script:RemoteHome
}

function Get-MacRepoDir {
    # Lokaler Checkout unter macOS, Remote-Checkout unter Windows.
    if ($isMacOS) { return $repoRoot }
    return $RemoteRepoPath
}

function Sync-RemoteCheckout {
    # Bringt den Remote-Checkout auf den lokalen HEAD-Stand, damit der
    # Remote-Build exakt den lokalen Quellstand kompiliert.
    if ($isMacOS) { return }
    $sha = (& git -C $repoRoot rev-parse HEAD 2>$null).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $sha) {
        Write-Host "Fehler: lokaler HEAD-Commit konnte nicht bestimmt werden." -ForegroundColor Red
        exit 1
    }
    Write-Host "Synchronisiere Remote-Checkout $RemoteRepoPath auf $sha ..." -ForegroundColor Cyan
    $check = Invoke-OnMac -Script "test -d '$RemoteRepoPath/.git'" -Description "Remote-Repo pruefen" -ReturnExitCode
    if ($check -ne 0) {
        Write-Host "Fehler: Kein Repo-Checkout unter $RemoteRepoPath auf dem Mac gefunden." -ForegroundColor Red
        Write-Host "Lege dort einen Clone an (z. B. git clone <url> $RemoteRepoPath) oder" -ForegroundColor Yellow
        Write-Host "passe -RemoteRepoPath / VIDEOPLAYER_TVOS_MAC_REPO_PATH an." -ForegroundColor Yellow
        exit 1
    }
    Invoke-OnMac -Script @"
set -e
cd "$RemoteRepoPath"
git fetch -q origin
git checkout -q "$sha"
git submodule -q update --init 2>/dev/null || true
"@ -Description "Remote-Checkout synchronisieren"
}

function Get-RemoteIdentities {
    $lines = Invoke-MacCapture -Script 'security find-identity -v -p codesigning | sed -n ''s/.*"\(.*\)".*/\1/p'''
    return @($lines | Where-Object { $_ })
}

function Get-RemoteProfiles {
    $script = @'
for f in "$HOME/Library/MobileDevice/Provisioning Profiles"/*.mobileprovision; do
  [ -e "$f" ] || continue
  xml=$(security cms -D -i "$f" 2>/dev/null) || continue
  name=$(printf '%s' "$xml" | plutil -extract Name raw -o - - 2>/dev/null)
  [ -z "$name" ] && name=$(basename "$f" .mobileprovision)
  gta=$(printf '%s' "$xml" | plutil -extract Entitlements.get-task-allow raw -o - - 2>/dev/null)
  printf '%s|%s\n' "$name" "$gta"
done
'@
    $lines = Invoke-MacCapture -Script $script
    # Mehrere .mobileprovision-Dateien koennen denselben Namen tragen (jeder Download
    # legt eine neue UUID-Datei an) -> nach Name deduplizieren.
    return @($lines | Where-Object { $_ } | ForEach-Object {
        $parts = $_ -split '\|', 2
        [pscustomobject]@{ Name = $parts[0]; DevProfile = ($parts[1] -eq 'true') }
    } | Sort-Object Name -Unique)
}

function Get-RemoteDevices {
    $out = (Invoke-MacCapture -Script 'T=$(mktemp); xcrun devicectl list devices -j "$T" >/dev/null 2>&1 && cat "$T"; rm -f "$T"') -join "`n"
    if (-not $out) { return @() }
    try {
        $devs = ($out | ConvertFrom-Json).result.devices
    } catch { return @() }
    return @($devs | ForEach-Object {
        [pscustomobject]@{
            Identifier = $_.identifier
            Name = $_.deviceProperties.name
            State = $_.connectionProperties.tunnelState
        }
    } | Where-Object { $_.Identifier })
}

function Select-FromList {
    param([string]$Title, [array]$Options, [string]$Default = "")
    if ($Options.Count -eq 0) { return $null }
    Write-Host $Title -ForegroundColor Cyan
    for ($i = 0; $i -lt $Options.Count; $i++) {
        $marker = if ($Options[$i] -eq $Default) { " [Standard]" } else { "" }
        Write-Host ("  [{0}] {1}{2}" -f ($i + 1), $Options[$i], $marker)
    }
    $hint = if ($Default) { "Enter uebernimmt Standard" } else { "" }
    $sel = Read-Host ("Auswahl ({0}1-{1})" -f $(if ($hint) { "$hint, " } else { "" }), $Options.Count)
    if ([string]::IsNullOrWhiteSpace($sel) -and $Default) { return $Default }
    $idx = 0
    if ([int]::TryParse($sel, [ref]$idx) -and $idx -ge 1 -and $idx -le $Options.Count) { return $Options[$idx - 1] }
    Write-Host "Ungueltige Auswahl: '$sel'" -ForegroundColor Red
    return Select-FromList -Title $Title -Options $Options -Default $default
}

function Get-DeployPrefsPath { return (Join-Path $repoRoot ".tvos-deploy.user.json") }

function Get-DeployPrefs {
    $p = Get-DeployPrefsPath
    if (Test-Path $p) {
        try { return Get-Content $p -Raw | ConvertFrom-Json } catch { return $null }
    }
    return $null
}

function Save-DeployPrefs {
    param([string]$Action)
    $prefs = Get-DeployPrefs
    if (-not $prefs) { $prefs = [pscustomobject]@{} }
    $entry = [pscustomobject]@{ CodesignKey = $CodesignKey; CodesignProvision = $CodesignProvision; Device = $Device }
    if ($prefs.PSObject.Properties.Name -contains $Action) {
        $prefs.$Action = $entry
    }
    else {
        $prefs | Add-Member -NotePropertyName $Action -NotePropertyValue $entry
    }
    $prefs | ConvertTo-Json -Depth 5 | Set-Content (Get-DeployPrefsPath) -Encoding UTF8
}

function Resolve-CodesigningInteractive {
    param([string]$Action)
    if ($Action -eq "simulator" -or $Action -eq "list" -or $Action -eq "menu" -or $Action -eq "build") { return }
    if ($NoPrompt) { return }
    if (-not $isMacOS -and -not ($ServerAddress -and $ServerUser)) { return }

    $isStore = $Action -in @('store', 'upload')

    # VIDEOPLAYER_TVOS_CODESIGN_KEY / VIDEOPLAYER_TVOS_PROVISIONING_PROFILE sind
    # fuer den Store-Upload gedacht (sie enthalten dort das Distribution-Profil).
    # Beim lokalen Geraete-Deployment wird die Auswahl zwingend angeboten,
    # damit kein Store-Profil still in den Device-Pfad rutscht.
    # Explizit uebergebene -CodesignKey/-CodesignProvision bleiben bestehen.
    if ($Action -eq 'device') {
        if ($env:VIDEOPLAYER_TVOS_PROVISIONING_PROFILE -and
            $CodesignProvision -eq $env:VIDEOPLAYER_TVOS_PROVISIONING_PROFILE) {
            Write-Host "Hinweis: VIDEOPLAYER_TVOS_PROVISIONING_PROFILE ('$CodesignProvision') gilt fuer den Store-Upload - Geraete-Deployment fragt das Profil ab." -ForegroundColor Yellow
            $script:CodesignProvision = ""
        }
        if ($env:VIDEOPLAYER_TVOS_CODESIGN_KEY -and
            $CodesignKey -eq $env:VIDEOPLAYER_TVOS_CODESIGN_KEY) {
            $script:CodesignKey = ""
        }
        if ($CodesignKey -like 'Apple Distribution:*') {
            Write-Host "Hinweis: '$CodesignKey' kann nicht auf Geraeten installiert werden - Auswahl wird angeboten." -ForegroundColor Yellow
            $script:CodesignKey = ""
            $script:CodesignProvision = ""
        }
    }

    $prefs = Get-DeployPrefs
    $saved = if ($prefs -and ($prefs.PSObject.Properties.Name -contains $Action)) { $prefs.$Action } else { $null }

    if ($CodesignKey -and $CodesignProvision) {
        if (-not $saved) { Save-DeployPrefs -Action $Action }
        return
    }

    if (-not $CodesignKey) {
        $ids = @(Get-RemoteIdentities)
        if ($isStore) { $ids = @($ids | Where-Object { $_ -like 'Apple Distribution:*' }) }
        elseif ($Action -eq 'device') { $ids = @($ids | Where-Object { $_ -like 'Apple Development:*' }) }
        if ($ids.Count -eq 0) {
            Write-Host "Warnung: Keine passenden Signaturidentitaeten auf dem Mac gefunden." -ForegroundColor Yellow
        }
        else {
            $default = if ($saved -and $saved.CodesignKey -and ($ids -contains $saved.CodesignKey)) { $saved.CodesignKey } else { $ids[0] }
            $script:CodesignKey = Select-FromList -Title "Signaturzertifikat waehlen:" -Options $ids -Default $default
        }
    }
    if (-not $CodesignProvision) {
        $profs = @(Get-RemoteProfiles)
        if ($isStore) { $profs = @($profs | Where-Object { -not $_.DevProfile }) }
        elseif ($Action -eq 'device') { $profs = @($profs | Where-Object { $_.DevProfile }) }
        $names = @($profs | ForEach-Object { $_.Name })
        if ($names.Count -eq 0) {
            Write-Host "Warnung: Keine passenden Provisioning-Profile auf dem Mac gefunden." -ForegroundColor Yellow
        }
        else {
            $default = if ($saved -and $saved.CodesignProvision -and ($names -contains $saved.CodesignProvision)) { $saved.CodesignProvision } else { $names[0] }
            $script:CodesignProvision = Select-FromList -Title "Bereitstellungsprofil waehlen:" -Options $names -Default $default
        }
    }
    if ($CodesignKey -and $CodesignProvision) { Save-DeployPrefs -Action $Action }
}

function Assert-CodesigningForAction {
    param([string]$Action)
    if ($Action -eq "simulator" -or $Action -eq "build") { return }
    Resolve-CodesigningInteractive -Action $Action
    if (-not $CodesignKey) {
        Write-Host "Fehler: -CodesignKey bzw. VIDEOPLAYER_TVOS_CODESIGN_KEY ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
    if (-not $CodesignProvision) {
        Write-Host "Fehler: -CodesignProvision bzw. VIDEOPLAYER_TVOS_PROVISIONING_PROFILE ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
}

$script:TranscriptStarted = $false

function Start-DeployLog {
    if ($script:TranscriptStarted) { return }
    try {
        $dir = if ([IO.Path]::IsPathRooted($LogDir)) { $LogDir } else { Join-Path $repoRoot $LogDir }
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
        $logPath = Join-Path $dir "tvos-deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
        Start-Transcript -Path $logPath -Append | Out-Null
        $script:TranscriptStarted = $true
        Write-Host "Log: $logPath" -ForegroundColor Gray
    }
    catch {
        Write-Host "Warnung: Transcript-Logging nicht verfuegbar: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Stop-DeployLog {
    if ($script:TranscriptStarted) {
        try { Stop-Transcript | Out-Null } catch { }
    }
}

function Assert-StorePrerequisites {
    param([string]$Action)
    Resolve-CodesigningInteractive -Action $Action
    $missing = @()
    if (-not $CodesignKey) { $missing += "-CodesignKey / VIDEOPLAYER_TVOS_CODESIGN_KEY" }
    if (-not $CodesignProvision) { $missing += "-CodesignProvision / VIDEOPLAYER_TVOS_PROVISIONING_PROFILE" }
    if (-not $ApiKeyPath) { $missing += "-ApiKeyPath / VIDEOPLAYER_TVOS_API_KEY_PATH" }
    if (-not $ApiKeyId) { $missing += "-ApiKeyId / VIDEOPLAYER_TVOS_API_KEY_ID" }
    if (-not $ApiIssuerId) { $missing += "-ApiIssuerId / VIDEOPLAYER_TVOS_API_ISSUER_ID" }
    if ($missing) {
        Write-Host "Fehler: Fehlende Pflichtparameter fuer den Store-Upload:" -ForegroundColor Red
        foreach ($m in $missing) { Write-Host "  $m" -ForegroundColor Red }
        exit 1
    }
    if ($CodesignKey -match 'Development') {
        Write-Host "Fehler: -CodesignKey ('$CodesignKey') ist ein Development-Zertifikat." -ForegroundColor Red
        Write-Host "Fuer App Store/TestFlight wird ein 'Apple Distribution'-Zertifikat benoetigt." -ForegroundColor Yellow
        exit 1
    }
    try {
        $resolvedKey = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ApiKeyPath)
    } catch {
        Write-Host "Fehler: API-Key-Pfad ungueltig: $ApiKeyPath ($($_.Exception.Message))" -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path $resolvedKey)) {
        Write-Host "Fehler: API-Key-Datei nicht gefunden: $resolvedKey" -ForegroundColor Red
        exit 1
    }
    if ($resolvedKey -notlike "*.p8") {
        Write-Host "Fehler: -ApiKeyPath muss auf eine .p8-Datei zeigen: $resolvedKey" -ForegroundColor Red
        exit 1
    }
    $repoRootWithSep = $repoRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if ($resolvedKey.StartsWith($repoRootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Fehler: Der .p8-API-Key darf nicht innerhalb des Repository liegen ($repoRoot)." -ForegroundColor Red
        Write-Host "Lege die Datei z. B. unter `$HOME/.appstoreconnect/private_keys/ ab." -ForegroundColor Yellow
        exit 1
    }
    if ($isWindows -and (-not $ServerAddress -or -not $ServerUser)) {
        Write-Host "Fehler: Unter Windows wird fuer store/upload ein SSH-Zugang zum Mac benoetigt." -ForegroundColor Red
        Write-Host "Setze -ServerAddress / -ServerUser bzw. VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS / VIDEOPLAYER_TVOS_MAC_SERVER_USER." -ForegroundColor Yellow
        Write-Host "Voraussetzung: schluesselbasiertes SSH (kein Passwort-Prompt)." -ForegroundColor Yellow
        exit 1
    }
}

function Get-TransporterFindScript {
    # Bash-Snippet, das ITMS auf einen ausfuehrbaren iTMSTransporter setzt.
    # Seit Xcode 16 liefert Xcode nur einen Stub - die echten Orte sind
    # die Transporter.app und das Standalone-Paket unter /usr/local/itms
    # (gleiche Kandidatenliste wie .github/actions/package-tvos).
    return @'
ITMS=""
CANDIDATES=(
  "/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter"
  "$HOME/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter"
  "/usr/local/itms/bin/iTMSTransporter"
)
DEVDIR="$(xcode-select -p 2>/dev/null || true)"
if [ -n "$DEVDIR" ]; then
  CANDIDATES+=("$DEVDIR/../SharedFrameworks/ContentDeliveryServices.framework/Versions/A/itms/bin/iTMSTransporter")
fi
for c in "${CANDIDATES[@]}"; do
  if [ -x "$c" ]; then ITMS="$c"; break; fi
done
'@
}

function Assert-TransporterAvailable {
    $check = (Get-TransporterFindScript) + "`n" + '[ -n "$ITMS" ]'
    $code = Invoke-OnMac -Script $check -Description "iTMSTransporter-Verfuegbarkeit" -ReturnExitCode
    if ($code -ne 0) {
        Write-Host "Fehler: iTMSTransporter auf dem Mac nicht gefunden." -ForegroundColor Red
        Write-Host "Seit Xcode 16 gehoert es zur Transporter-App - aus dem Mac App Store installieren:" -ForegroundColor Yellow
        Write-Host "https://apps.apple.com/us/app/transporter/id1450874784" -ForegroundColor Yellow
        exit 1
    }
}

function Get-CommitBuildNumber {
    # CFBundleVersion = Anzahl der Commits - dieselbe Zaehlung wie die
    # CI-Pipeline (.github/actions/package-tvos). Repo-weit monoton, das
    # csproj bleibt unveraendert (kein lokaler Zaehler-Stand, keine
    # Dirty-Diffs nach jedem Store-Lauf). Apple verlangt je Upload eine
    # neue Nummer: bei einem erneuten Upload desselben Commits
    # -BuildNumber mit einem groesseren Wert uebergeben.
    $count = & git -C $repoRoot rev-list --count HEAD 2>$null
    if ($LASTEXITCODE -ne 0 -or -not ($count -match '^\d+$')) {
        Write-Host "Warnung: Commit-Anzahl nicht bestimmbar - csproj-Wert wird verwendet." -ForegroundColor Yellow
        return $null
    }
    return $count
}

function Get-LocalBundleId {
    $plist = Join-Path $repoRoot "VideoPlayer.TvOS/Info.plist"
    $csproj = [xml](Get-Content (Join-Path $repoRoot "VideoPlayer.TvOS/VideoPlayer.TvOS.csproj"))
    $appId = ($csproj.Project.PropertyGroup | ForEach-Object { $_.ApplicationId } | Where-Object { $_ } | Select-Object -First 1)
    if (-not $appId) {
        Write-Host "Fehler: ApplicationId konnte nicht aus der csproj gelesen werden." -ForegroundColor Red
        exit 1
    }
    return $appId
}

function Get-LatestIpa {
    if ($IpaPath) {
        try {
            $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($IpaPath)
        } catch {
            Write-Host "Fehler: -IpaPath ungueltig: $IpaPath ($($_.Exception.Message))" -ForegroundColor Red
            exit 1
        }
        if (-not (Test-Path $resolved)) {
            Write-Host "Fehler: -IpaPath nicht gefunden: $resolved" -ForegroundColor Red
            exit 1
        }
        return $resolved
    }
    $config = Get-Configuration -Action "store"
    $rid = Get-RuntimeIdentifier -Action "store"
    $dir = Join-Path $repoRoot "VideoPlayer.TvOS/bin/$config/$framework/$rid"
    $ipas = Get-ChildItem -Path $dir -Recurse -Filter "*.ipa" -ErrorAction SilentlyContinue
    if (-not $ipas) {
        # Ablage des Ruecktransfers von Remote-Builds (Windows).
        $dir = Join-Path $repoRoot "artifacts/tvos/$config/$framework/$rid"
        $ipas = Get-ChildItem -Path $dir -Recurse -Filter "*.ipa" -ErrorAction SilentlyContinue
    }
    if (-not $ipas) {
        Write-Host "Fehler: Keine .ipa unter $dir gefunden." -ForegroundColor Red
        exit 1
    }
    $names = $ipas | ForEach-Object { $_.Name } | Sort-Object -Unique
    if ($names.Count -gt 1) {
        Write-Host "Fehler: Mehrere verschiedene .ipa-Dateien unter ${dir}:" -ForegroundColor Red
        $names | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        exit 1
    }
    $preferred = $ipas | Where-Object { $_.FullName -match 'publish' } | Select-Object -First 1
    if (-not $preferred) { $preferred = $ipas | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }
    return $preferred.FullName
}

function Copy-FileToMac {
    param(
        [string]$LocalPath,
        [string]$RemotePath
    )
    if ($isMacOS) { return }
    # Windows-scp nutzt SFTP-Backend: kein Remote-Shell-Expand von $HOME/~ -> absoluten Pfad aufloesen
    if ($RemotePath -like '`$HOME/*' -or $RemotePath -like '~/*') {
        $RemotePath = (Get-RemoteHome) + $RemotePath.Substring($RemotePath.IndexOf('/'))
    }
    $scpArgs = @("-o", "BatchMode=yes", "-o", "ConnectTimeout=10")
    if (Test-Path $LocalPath -PathType Container) { $scpArgs += "-r" }
    & scp @scpArgs $LocalPath "${ServerUser}@${ServerAddress}:$RemotePath"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: scp nach ${ServerUser}@${ServerAddress}:$RemotePath fehlgeschlagen." -ForegroundColor Red
        exit 1
    }
}

function Copy-FileFromMac {
    param(
        [string]$RemotePath,
        [string]$LocalPath
    )
    if ($isMacOS) { return }
    if ($RemotePath -like '`$HOME/*' -or $RemotePath -like '~/*') {
        $RemotePath = (Get-RemoteHome) + $RemotePath.Substring($RemotePath.IndexOf('/'))
    }
    $dir = Split-Path -Parent $LocalPath
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    & scp -o BatchMode=yes -o ConnectTimeout=10 "${ServerUser}@${ServerAddress}:$RemotePath" $LocalPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: scp von ${ServerUser}@${ServerAddress}:$RemotePath fehlgeschlagen." -ForegroundColor Red
        exit 1
    }
}

function Copy-ApiKeyToMac {
    $remoteDir = '$HOME/.appstoreconnect/private_keys'
    $remoteKey = "$remoteDir/AuthKey_$ApiKeyId.p8"
    $check = Invoke-OnMac -Script "test -f $remoteKey" -Description "API-Key-Existenz pruefen" -ReturnExitCode
    if ($check -eq 0) {
        Write-Host "API-Key bereits auf dem Mac vorhanden: $remoteKey" -ForegroundColor Gray
        return $remoteKey
    }
    Invoke-OnMac -Script 'mkdir -p "$HOME/.appstoreconnect/private_keys" && chmod 700 "$HOME/.appstoreconnect/private_keys"' -Description "Anlegen des API-Key-Verzeichnisses"
    $localKey = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ApiKeyPath)
    if ($isMacOS) {
        Copy-Item $localKey "$HOME/.appstoreconnect/private_keys/AuthKey_$ApiKeyId.p8" -Force
    }
    else {
        Copy-FileToMac -LocalPath $localKey -RemotePath $remoteKey
    }
    Write-Host "API-Key bereitgestellt: $remoteKey" -ForegroundColor Green
    return $remoteKey
}

function Get-MacIpaPath {
    param([string]$LocalIpa)
    if ($isMacOS) { return $LocalIpa }
    $name = Split-Path $LocalIpa -Leaf
    $remote = '$HOME/tvos-uploads/' + $name
    Invoke-OnMac -Script 'mkdir -p "$HOME/tvos-uploads"' -Description "Anlegen des Upload-Verzeichnisses"
    Write-Host "Kopiere IPA auf den Mac: $remote" -ForegroundColor Cyan
    Copy-FileToMac -LocalPath $LocalIpa -RemotePath $remote
    return $remote
}

function Get-WorkloadScript {
    # tvOS-Manifest pinnen wie die CI-Jobs (Rollback + --skip-manifest-update).
    return @"
set -e
TMPDIR_R=`$(mktemp -d)
trap 'rm -rf "`$TMPDIR_R"' EXIT
printf '{"microsoft.net.sdk.tvos": "%s"}\n' "$workloadVersion" > "`$TMPDIR_R/workload-rollback.json"
dotnet workload update --from-rollback-file "`$TMPDIR_R/workload-rollback.json"
dotnet workload install tvos --skip-manifest-update
"@
}

function Invoke-IpaValidation {
    param([string]$MacIpa)
    $transporterFind = Get-TransporterFindScript
    $macScript = @"
set -e
IPA="$MacIpa"
echo "==> Entpacke IPA zur Signaturpruefung"
TMP=`$(mktemp -d)
trap 'rm -rf "`$TMP"' EXIT
ditto -x -k "`$IPA" "`$TMP" 2>/dev/null || unzip -q "`$IPA" -d "`$TMP"
APP=`$(find "`$TMP/Payload" -maxdepth 1 -name '*.app' | head -1)
if [ -z "`$APP" ]; then echo "Kein .app-Bundle in der IPA gefunden"; exit 1; fi
echo "==> codesign --verify"
codesign --verify --deep --strict -vvv "`$APP"
echo "==> Pruefe embedded.mobileprovision"
PROFILE_XML=`$(security cms -D -i "`$APP/embedded.mobileprovision")
if echo "`$PROFILE_XML" | grep -A1 'get-task-allow' | grep -q '<true/>'; then
    echo "FEHLER: Development-Profil (get-task-allow=true) - nicht store-tauglich"; exit 1
fi
echo "==> Pruefe PrivacyInfo.xcprivacy"
# Die App deklariert Required-Reason-APIs; fehlt das Manifest im Bundle-Root,
# meldet die Verarbeitung ITMS-91053 - das ist ein Packaging-Fehler.
if [ ! -f "`$APP/PrivacyInfo.xcprivacy" ]; then
    echo "FEHLER: PrivacyInfo.xcprivacy fehlt im Bundle-Root"; exit 1
fi
plutil -lint "`$APP/PrivacyInfo.xcprivacy" >/dev/null 2>&1 || { echo "FEHLER: PrivacyInfo.xcprivacy ist kein gueltiges plist"; exit 1; }
echo "==> Pruefe Bundle-Metadaten (Info.plist)"
# Invarianten der Store-Konfiguration: Apple-TV-Familie (3) und keine
# nicht-freigestellte Verschluesselung.
FAMILY=`$(plutil -extract UIDeviceFamily json -o - "`$APP/Info.plist" 2>/dev/null || echo '[]')
echo "`$FAMILY" | grep -q '3' || { echo "FEHLER: UIDeviceFamily ohne Apple TV (3): `$FAMILY"; exit 1; }
ENC=`$(plutil -extract ITSAppUsesNonExemptEncryption raw -o - "`$APP/Info.plist" 2>/dev/null || echo '?')
[ "`$ENC" = "false" ] || [ "`$ENC" = "0" ] || { echo "FEHLER: ITSAppUsesNonExemptEncryption nicht false: `$ENC"; exit 1; }
echo "==> iTMSTransporter -m verify"
# iTMSTransporter nutzt dieselbe API-Key-Authentifizierung und denselben
# Schluesselsuchpfad ~/.appstoreconnect/private_keys. Seit Xcode 16 gehoert
# das Werkzeug zur Transporter-App aus dem Mac App Store. Fuer Apps
# (.ipa) verlangt iTMSTransporter -assetFile; -f gilt nur fuer
# .itmsp-Pakete (Apple Transporter User Guide). Falls das Werkzeug fehlt
# oder -m verify nicht unterstuetzt wird, entfaellt die Remote-
# Validierung: die codesign-Pruefung oben ist bestanden und der Upload
# validiert serverseitig (dokumentierter Fallback).
$transporterFind
if [ -z "`$ITMS" ] || [ ! -x "`$ITMS" ]; then
    echo "WARNUNG: iTMSTransporter nicht gefunden - Transporter-App aus dem Mac App Store installieren. Remote-Validierung entfaellt, der Upload validiert serverseitig (Fallback)."
elif ! "`$ITMS" -m verify -assetFile "`$IPA" -apiKey "$ApiKeyId" -apiIssuer "$ApiIssuerId"; then
    echo "WARNUNG: iTMSTransporter -m verify fehlgeschlagen oder nicht unterstuetzt - Remote-Validierung entfaellt, der Upload validiert serverseitig (Fallback)."
fi
echo "==> Validierung abgeschlossen"
"@
    Invoke-OnMac -Script $macScript -Description "IPA-Validierung"
}

function Invoke-StoreUpload {
    param([string]$MacIpa)
    $transporterFind = Get-TransporterFindScript
    $macScript = @"
set -e
# -assetFile statt -f: -f ist fuer .itmsp-Pakete reserviert und darf fuer
# App-Uploads nicht verwendet werden (Apple Transporter User Guide).
$transporterFind
if [ -z "`$ITMS" ] || [ ! -x "`$ITMS" ]; then
    echo "FEHLER: iTMSTransporter nicht gefunden - Transporter-App aus dem Mac App Store installieren."
    exit 1
fi
"`$ITMS" -m upload -assetFile "$MacIpa" -apiKey "$ApiKeyId" -apiIssuer "$ApiIssuerId"
echo "==> Upload erfolgreich - der Build erscheint nach der Verarbeitung in App Store Connect / TestFlight"
"@
    Invoke-OnMac -Script $macScript -Description "App-Store-Upload"
}

function Invoke-Build {
    param(
        [string]$Action,
        [string]$ApplicationVersionOverride = ""
    )
    # Kompiliert auf dem Mac (lokal oder via SSH im Remote-Checkout).
    # Rueckgabe: Mac-Pfad der erzeugten .ipa bzw. .app.
    $rid = Get-RuntimeIdentifier -Action $Action
    $config = Get-Configuration -Action $Action
    $hasCodesigning = $CodesignKey -and $CodesignProvision
    $repoDir = Get-MacRepoDir
    $workload = Get-WorkloadScript

    if ($hasCodesigning) {
        $verb = "publish"
        $signProps = "-p:CodesignKey=`"$CodesignKey`" -p:CodesignProvision=`"$CodesignProvision`" -p:ArchiveOnBuild=true"
        if ($CodesignEntitlements) { $signProps += " -p:CodesignEntitlements=`"$CodesignEntitlements`"" }
        if ($ApplicationVersionOverride) { $signProps += " -p:ApplicationVersion=`"$ApplicationVersionOverride`"" }
        Write-Host "Erzeuge signiertes tvOS-Kompilat (.ipa) ..." -ForegroundColor Cyan
    }
    else {
        $verb = "build"
        $signProps = "-p:EnableCodeSigning=false"
        Write-Host "Baue tvOS-App ohne Codesigning ($config/$rid) ..." -ForegroundColor Cyan
    }

    $buildScript = @"
set -e
cd "$repoDir"
$workload
dotnet $verb $projectPath -f $framework -c $config -r $rid -p:IncludeTvOsTarget=true $signProps
# Artefaktpfad ausgeben (Marker fuer den Aufrufer)
OUTDIR="VideoPlayer.TvOS/bin/$config/$framework/$rid"
if [ "$verb" = "publish" ]; then
  ART=`$(find "`$OUTDIR" -name '*.ipa' | sort | head -1)
else
  ART=`$(find "`$OUTDIR" -name '*.app' -type d | sort | head -1)
fi
[ -n "`$ART" ] || { echo "Kein Artefakt unter `$OUTDIR gefunden" >&2; exit 1; }
echo "TVOS_ARTIFACT=`$ART"
"@
    $output = Invoke-MacCapture -Script $buildScript
    $output | ForEach-Object { Write-Host $_ }
    $artifactLine = $output | Where-Object { $_ -match '^TVOS_ARTIFACT=' } | Select-Object -Last 1
    if (-not $artifactLine) {
        Write-Host "Fehler: Build fehlgeschlagen oder Artefakt nicht gefunden." -ForegroundColor Red
        exit 1
    }
    $artifact = ($artifactLine -replace '^TVOS_ARTIFACT=', '').Trim()
    $macPath = if ($artifact.StartsWith('/')) { $artifact } else { "$repoDir/$artifact" }
    Write-Host "Artefakt (Mac): $macPath" -ForegroundColor Green
    return $macPath
}

function Copy-IpaBackToLocal {
    # Zieht eine remote gebaute .ipa nach Windows zurueck (artifacts/, gitignored).
    param([string]$MacIpa)
    if ($isMacOS -or -not $MacIpa.EndsWith('.ipa')) { return $MacIpa }
    $name = Split-Path $MacIpa -Leaf
    $config = Get-Configuration -Action "store"
    $rid = Get-RuntimeIdentifier -Action "store"
    $local = Join-Path $repoRoot "artifacts/tvos/$config/$framework/$rid/$name"
    Write-Host "Kopiere IPA zurueck nach $local ..." -ForegroundColor Cyan
    Copy-FileFromMac -RemotePath $MacIpa -LocalPath $local
    return $local
}

function Export-DsymArchive {
    # Legt die dSYM-Bundles des Build-Outputs als Zip neben der .ipa ab,
    # damit Crash-Reports des Builds spaeter symbolisiert werden koennen.
    # Laeuft auf dem Mac; bei Remote-Builds bleibt das Archiv dort.
    param(
        [string]$MacIpa,
        [string]$Config,
        [string]$Rid
    )
    $repoDir = Get-MacRepoDir
    $macScript = @"
set -e
cd "$repoDir"
BIN="VideoPlayer.TvOS/bin/$Config/$framework/$Rid"
DSYMS=`$(find "`$BIN" -name '*.dSYM' -type d 2>/dev/null || true)
if [ -z "`$DSYMS" ]; then
  echo "Keine .dSYM-Bundles unter `$BIN gefunden - kein Symbolisierungsarchiv erzeugt."
  exit 0
fi
case "$MacIpa" in
  /*) IPADIR="`$(dirname "$MacIpa")" ;;
  *)  IPADIR="$repoDir/`$(dirname "$MacIpa")" ;;
esac
ZIP="`$IPADIR/`$(basename "$MacIpa" .ipa)-dsym.zip"
TMP=`$(mktemp -d)
trap 'rm -rf "`$TMP"' EXIT
find "`$BIN" -name '*.dSYM' -type d -exec cp -R {} "`$TMP/" \;
(cd "`$TMP" && zip -qr "`$ZIP" .)
echo "DSYM=`$ZIP"
"@
    $output = Invoke-MacCapture -Script $macScript
    $output | ForEach-Object { Write-Host $_ }
}

function Invoke-Store {
    Assert-StorePrerequisites -Action "store"
    Assert-CodesigningForAction -Action "store"
    Assert-TransporterAvailable
    Sync-RemoteCheckout
    $applicationVersion = $null
    if (-not $NoBumpBuildNumber) {
        $applicationVersion = if ($BuildNumber -gt 0) { "$BuildNumber" } else { Get-CommitBuildNumber }
        if ($applicationVersion) {
            Write-Host "Buildnummer (CFBundleVersion): $applicationVersion (Commit-Anzahl)" -ForegroundColor Green
        }
    }
    $macIpa = Invoke-Build -Action "store" -ApplicationVersionOverride $applicationVersion
    $rid = Get-RuntimeIdentifier -Action "store"
    $config = Get-Configuration -Action "store"
    Export-DsymArchive -MacIpa $macIpa -Config $config -Rid $rid
    Copy-IpaBackToLocal -MacIpa $macIpa | Out-Null
    Copy-ApiKeyToMac | Out-Null
    Invoke-IpaValidation -MacIpa $macIpa
    Invoke-StoreUpload -MacIpa $macIpa
    Write-Host "Store-Lauf abgeschlossen." -ForegroundColor Green
}

function Invoke-Upload {
    Assert-StorePrerequisites -Action "upload"
    Assert-TransporterAvailable
    $ipa = Get-LatestIpa
    Write-Host "IPA: $ipa" -ForegroundColor Green
    Copy-ApiKeyToMac | Out-Null
    $macIpa = Get-MacIpaPath -LocalIpa $ipa
    Invoke-IpaValidation -MacIpa $macIpa
    Invoke-StoreUpload -MacIpa $macIpa
    Write-Host "Upload abgeschlossen." -ForegroundColor Green
}

function Invoke-SimulatorMac {
    Sync-RemoteCheckout
    $rid = Get-RuntimeIdentifier -Action "simulator"
    $config = Get-Configuration -Action "simulator"
    $repoDir = Get-MacRepoDir
    $appMac = Invoke-Build -Action "simulator"

    if (-not $Device) {
        # Ersten verfuegbaren Apple-TV-Simulator waehlen: die Textausgabe
        # von simctl traegt die UDID in Klammern - robust ohne zusaetzliche
        # Werkzeug-Abhaengigkeiten (kein python3/jq vorausgesetzt).
        $udidLines = Invoke-MacCapture -Script @'
xcrun simctl list devices available | grep -i 'apple tv' | head -1 \
  | grep -oE '[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}' | head -1
'@
        $Device = ($udidLines | Select-Object -Last 1).Trim()
        if (-not $Device) {
            Write-Host "Fehler: Kein verfuegbarer Apple-TV-Simulator gefunden." -ForegroundColor Red
            Write-Host "Bitte -Device mit einer UDID angeben oder einen Simulator in Xcode erstellen." -ForegroundColor Yellow
            exit 1
        }
        Write-Host "Verwende Simulator-UDID: $Device" -ForegroundColor Green
    }

    $shotName = "simulator-screenshot-$(Get-Date -Format 'yyyyMMdd-HHmmss').png"
    $macScript = @"
set -e
xcrun simctl boot "$Device" 2>/dev/null || true
APP="$appMac"
PLIST="`$APP/Info.plist"
BUNDLE_ID=`$(/usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' "`$PLIST")
echo "Bundle-ID: `$BUNDLE_ID"
# Simulator-Build (EnableCodeSigning=false) hinterlaesst unsignierte oder
# ungueltig signierte Mach-O-Dateien im Bundle — u. a. .aotdata.arm64
# (von Mono per dlopen geladen) und .dylibs. Der Kernel toetet den Prozess
# sonst mit "Code Signature Invalid". Daher jede Mach-O-Datei einzeln
# adhoc-signieren (--deep allein erfasst .aotdata-Dateien nicht), danach
# das Bundle als Ganzes.
find "`$APP" -type f | while read f; do
  if file -b "`$f" | grep -q "Mach-O"; then codesign --force --sign - "`$f"; fi
done
# Keychain-Entitlement beim adhoc-Signieren mitschreiben: SecItem
# scheitert ohne keychain-access-groups mit errSecMissingEntitlement
# (Token-Persistenz fuer Pairing/Login). Adhoc-Signatur kennt kein
# Team-Prefix — die Default-Access-Group ist die Bundle-ID selbst;
# plutil ersetzt die `$(AppIdentifierPrefix)`$(CFBundleIdentifier)-
# Platzhalter des Templates entsprechend.
RESOLVED_ENT="/tmp/videos-tvos-entitlements.plist"
cp "$repoDir/VideoPlayer.TvOS/Entitlements.plist" "`$RESOLVED_ENT"
plutil -replace "keychain-access-groups.0" -string "`$BUNDLE_ID" "`$RESOLVED_ENT"
codesign --force --sign - --entitlements "`$RESOLVED_ENT" "`$APP"
xcrun simctl install "$Device" "`$APP" || { xcrun simctl uninstall "$Device" "`$BUNDLE_ID" || true; xcrun simctl install "$Device" "`$APP"; }
xcrun simctl launch "$Device" "`$BUNDLE_ID"
sleep 5
xcrun simctl io "$Device" screenshot "$repoDir/VideoPlayer.TvOS/bin/$config/$framework/$rid/$shotName" || true
echo "SCREENSHOT=$repoDir/VideoPlayer.TvOS/bin/$config/$framework/$rid/$shotName"
"@
    Invoke-OnMac -Script $macScript -Description "Simulator-Deployment"
    Write-Host "App im tvOS-Simulator gestartet." -ForegroundColor Green
}

function Invoke-Device {
    Assert-CodesigningForAction -Action "device"
    if (-not $Device) {
        $devs = @(Get-RemoteDevices)
        if ($devs.Count -eq 0) {
            Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID oder Name) angegeben werden." -ForegroundColor Red
            Write-Host "Keine angebundenen Geraete auf dem Mac gefunden (devicectl). Anzeigen: -Action list" -ForegroundColor Yellow
            exit 1
        }
        $options = @($devs | ForEach-Object { "$($_.Name) ($($_.Identifier)) [$($_.State)]" })
        $choice = Select-FromList -Title "Geraet waehlen:" -Options $options -Default $options[0]
        if (-not $choice) { exit 1 }
        $script:Device = $devs[$options.IndexOf($choice)].Identifier
    }
    Sync-RemoteCheckout
    $macIpa = Invoke-Build -Action "device"

    $bundleId = Get-LocalBundleId
    $installScript = @"
set -e
TMP=`$(mktemp -d)
trap 'rm -rf "`$TMP"' EXIT
ditto -x -k "$macIpa" "`$TMP" 2>/dev/null || unzip -q "$macIpa" -d "`$TMP"
APP=`$(find "`$TMP/Payload" -maxdepth 1 -name '*.app' | head -1)
if [ -z "`$APP" ]; then echo "Kein .app-Bundle in der IPA gefunden"; exit 1; fi
xcrun devicectl device install app --device "$Device" "`$APP"
"@
    Invoke-OnMac -Script $installScript -Description "Installation auf dem Geraet"

    if ($Console) {
        Write-Host "Starte App mit Console-Streaming (Ctrl+C loest die Verbindung, die App laeuft weiter) ..." -ForegroundColor Cyan
        Invoke-OnMac -Script "xcrun devicectl device process launch --device `"$Device`" --console $bundleId" -Description "App-Start mit Console" -ReturnExitCode | Out-Null
        return
    }
    Invoke-OnMac -Script "xcrun devicectl device process launch --device `"$Device`" $bundleId" -Description "App-Start auf dem Geraet"
    Write-Host "App gestartet. Tipp: -Console streamt die App-Ausgabe (inkl. Crash-Details) ins Terminal." -ForegroundColor Green
}

function Invoke-List {
    Assert-MacTarget
    Write-Host "== Signaturidentitaeten (codesigning) ==" -ForegroundColor Cyan
    $ids = @(Get-RemoteIdentities)
    if ($ids.Count -eq 0) { Write-Host "  (keine)" -ForegroundColor Yellow }
    $ids | ForEach-Object { Write-Host "  $_" }
    Write-Host "== Provisioning-Profile ==" -ForegroundColor Cyan
    $profs = @(Get-RemoteProfiles)
    if ($profs.Count -eq 0) { Write-Host "  (keine)" -ForegroundColor Yellow }
    $profs | ForEach-Object { Write-Host ("  {0}{1}" -f $_.Name, $(if ($_.DevProfile) { "  [Development]" } else { "" })) }
    Write-Host "== Geraete / Simulatoren ==" -ForegroundColor Cyan
    Invoke-OnMac -Script 'xcrun devicectl list devices 2>/dev/null || true; echo "--- Simulatoren ---"; xcrun simctl list devices available | grep -i "tv" || true' -Description "Geraete auflisten"
}

function Show-Menu {
    Write-Host ""
    Write-Host "================================"
    Write-Host "   tvOS Build & Deployment Menue"
    Write-Host "================================"
    Write-Host "1) Build (tvOS-App, optional .ipa mit Codesigning)"
    Write-Host "2) Build + tvOS-Simulator starten"
    Write-Host "3) Build + echten Apple TV deployen"
    Write-Host "4) Identitaeten/Profile/Geraete anzeigen"
    Write-Host "5) Release-Build + Upload zu App Store Connect (TestFlight)"
    Write-Host "6) Vorhandene .ipa validieren + hochladen"
    Write-Host "================================"
    $choice = Read-Host "Bitte waehlen (1-6)"

    switch ($choice) {
        "1" { $script:Action = "build" }
        "2" { $script:Action = "simulator" }
        "3" { $script:Action = "device" }
        "4" { $script:Action = "list" }
        "5" { $script:Action = "store" }
        "6" { $script:Action = "upload" }
        default {
            Write-Host "Ungueltige Auswahl." -ForegroundColor Red
            exit 1
        }
    }
}

if ($Action -eq "menu" -and -not $NoPrompt) { Show-Menu }

if ($Action -in @("build", "simulator", "device", "store", "upload")) { Start-DeployLog }

try {
    switch ($Action) {
        "build" {
            Assert-MacTarget
            Sync-RemoteCheckout
            $macArtifact = Invoke-Build -Action "build"
            if ($macArtifact.EndsWith('.ipa')) {
                $local = Copy-IpaBackToLocal -MacIpa $macArtifact
                Write-Host "IPA lokal: $local" -ForegroundColor Green
            }
        }
        "simulator" {
            Assert-MacTarget
            Invoke-SimulatorMac
        }
        "device" {
            Assert-MacTarget
            Invoke-Device
        }
        "store" {
            Assert-MacTarget
            Invoke-Store
        }
        "upload" {
            Invoke-Upload
        }
        "list" { Invoke-List }
        default {
            Write-Host "Unbekannte Aktion: $Action" -ForegroundColor Red
            exit 1
        }
    }
}
finally {
    Stop-DeployLog
}

exit 0
