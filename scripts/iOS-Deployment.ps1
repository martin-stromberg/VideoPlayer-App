<#
    iOS-Deployment.ps1

    Build und Deployment des .NET MAUI iOS-Teils von VideoPlayer.
    (Vorlage: Reporter/scripts/iOS-Deployment.ps1)

    Moegliche Aktionen:
        build     -> iOS-App bauen (mit Codesigning: .ipa)
        simulator -> App bauen, im iOS-Simulator starten und Screenshot speichern (nur auf macOS)
        device    -> App bauen und auf einem echten iOS-Geraet starten (nur auf macOS)
        store     -> Signierten Release-Build erzeugen, validieren und zu App Store Connect
                     hochladen (TestFlight). Erhoeht automatisch die Buildnummer
                     (Opt-out: -NoBumpBuildNumber). Auf Windows per SSH an den Mac delegiert.
        upload    -> Vorhandene .ipa validieren und zu App Store Connect hochladen
        list      -> Verfuegbare Simulatoren/Geraete anzeigen (nur macOS)
        menu      -> Interaktives Menue

    Windows mit Pair-to-Mac:
        -ServerAddress, -ServerUser, -ServerPassword setzen (oder Umgebungsvariablen)
        -_DotNetRootRemoteDirectory ist fuer VS 2022:
            /Users/<user>/Library/Caches/Xamarin/XMA/SDKs/dotnet/
         (fuer VS 2026:
            /Users/<user>/Library/Caches/maui/PairToMac/SDKs/dotnet/)
        Fuer store/upload von Windows aus wird zusaetzlich schluesselbasiertes SSH
        zum Mac benoetigt (BatchMode, kein Passwort-Prompt).

    Umgebungsvariablen:
        VIDEOPLAYER_IOS_CODESIGN_KEY
        VIDEOPLAYER_IOS_PROVISIONING_PROFILE
        VIDEOPLAYER_IOS_MAC_SERVER_ADDRESS
        VIDEOPLAYER_IOS_MAC_SERVER_USER
        VIDEOPLAYER_IOS_MAC_SERVER_PASSWORD
        VIDEOPLAYER_IOS_MAC_DOTNET_ROOT
        VIDEOPLAYER_IOS_API_KEY_PATH    -> Pfad zum App Store Connect API Key (.p8, ausserhalb des Repo)
        VIDEOPLAYER_IOS_API_KEY_ID      -> Key-ID aus App Store Connect
        VIDEOPLAYER_IOS_API_ISSUER_ID   -> Issuer-ID aus App Store Connect
        VIDEOWEBPLAYER_MAUI_API_TOKEN   -> statischer Client-Gate-Schluessel (Build-Eigenschaft VideoPlayerApiToken;
                                         Geraete ohne Pairing-Endpunkt/Alt-Clients). In der Anzeige maskiert.
#>

param(
    [ValidateSet("build", "simulator", "device", "store", "upload", "list", "menu")]
    [string]$Action = "menu",

    [Parameter(HelpMessage = "UDID oder Name des Simulators / Geraets. Beispiel Simulator: E25BBE37-69BA-4720-B6FD-D54C97791E79")]
    [string]$Device = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "",

    [Parameter(HelpMessage = "iossimulator-arm64 | iossimulator-x64 | ios-arm64")]
    [string]$RuntimeIdentifier = "",

    [string]$ServerAddress = $env:VIDEOPLAYER_IOS_MAC_SERVER_ADDRESS,
    [Parameter(HelpMessage = "Der macOS-Kurzname (z. B. martin), nicht der volle Benutzername.")]
    [string]$ServerUser = $env:VIDEOPLAYER_IOS_MAC_SERVER_USER,
    [string]$ServerPassword = $env:VIDEOPLAYER_IOS_MAC_SERVER_PASSWORD,
    [string]$TcpPort = "58181",
    [string]$DotNetRootRemoteDirectory = $env:VIDEOPLAYER_IOS_MAC_DOTNET_ROOT,

    [string]$CodesignKey = $env:VIDEOPLAYER_IOS_CODESIGN_KEY,
    [string]$CodesignProvision = $env:VIDEOPLAYER_IOS_PROVISIONING_PROFILE,
    [string]$CodesignEntitlements = "",

    [Parameter(HelpMessage = "Pfad zum App Store Connect API Key (.p8). Muss ausserhalb des Repository liegen.")]
    [string]$ApiKeyPath = $env:VIDEOPLAYER_IOS_API_KEY_PATH,
    [string]$ApiKeyId = $env:VIDEOPLAYER_IOS_API_KEY_ID,
    [string]$ApiIssuerId = $env:VIDEOPLAYER_IOS_API_ISSUER_ID,
    [Parameter(HelpMessage = "Pfad zu einer vorhandenen .ipa (nur Aktion 'upload').")]
    [string]$IpaPath = "",
    [Parameter(HelpMessage = "Bei 'device' via SSH: App-Output (--console) nach dem Start streamen (Ctrl+C zum Loesen).")]
    [switch]$Console,
    [switch]$NoBumpBuildNumber,
    [string]$LogDir = "logs",
    [Parameter(HelpMessage = "Statischer Client-Gate-Schluessel (MSBuild-Eigenschaft VideoPlayerApiToken); wird in der Anzeige maskiert.")]
    [string]$VideoPlayerApiToken = $env:VIDEOWEBPLAYER_MAUI_API_TOKEN,

    [switch]$NoPrompt
)

# Response-Datei-Hilfsfunktionen (Sanitizing, Maskierung, rsp-Erzeugung)
# liegen in einer eigenen Datei, damit sie einzeln testbar bleiben.
. (Join-Path $PSScriptRoot "iOS-Deployment.ResponseFile.ps1")

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "VideoPlayer.Maui/VideoPlayer.Maui.csproj"
$framework = "net10.0-ios"

$isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$isMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)

# Default fuer den Remote-.NET-Pfad auf dem Mac (VS 2022 Pair-to-Mac Cache).
# VS 2026 / neuere Pair-to-Mac-Versionen nutzen ggf.
# /Users/<user>/Library/Caches/maui/PairToMac/SDKs/dotnet/
if ($isWindows -and $ServerUser -and -not $DotNetRootRemoteDirectory) {
    $DotNetRootRemoteDirectory = "/Users/$ServerUser/Library/Caches/maui/PairToMac/SDKs/dotnet/"
}

function Get-RuntimeIdentifier {
    param([string]$Action)

    if ($RuntimeIdentifier) { return $RuntimeIdentifier }
    if ($Action -eq "simulator") {
        if ($isMacOS) {
            $arch = & uname -m
            if ($arch -eq "arm64") { return "iossimulator-arm64" }
        }
        return "iossimulator-x64"
    }
    return "ios-arm64"
}

function Get-Configuration {
    param([string]$Action)
    if ($Configuration) { return $Configuration }
    if ($Action -eq "simulator") { return "Debug" }
    return "Release"
}

function Get-SimulatorUdid {
    if ($Device) { return $Device }

    Write-Host "Suche verfuegbaren iPhone-Simulator..." -ForegroundColor Cyan
    $list = & xcrun simctl list devices available -j 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: xcrun simctl konnte keine Simulatoren auflisten." -ForegroundColor Red
        exit 1
    }

    $json = $list | ConvertFrom-Json
    $devices = $json.devices.PSObject.Properties.Value | ForEach-Object { $_ } | Where-Object {
        $_.isAvailable -and $_.deviceTypeIdentifier -match 'iPhone'
    } | Sort-Object name

    if (-not $devices) {
        Write-Host "Fehler: Kein verfuegbarer iPhone-Simulator gefunden." -ForegroundColor Red
        Write-Host "Bitte -Device mit einer UDID angeben oder einen Simulator in Xcode erstellen." -ForegroundColor Yellow
        exit 1
    }

    $selected = $devices | Select-Object -First 1
    Write-Host "Verwende Simulator: $($selected.name) ($($selected.udid))" -ForegroundColor Green
    return $selected.udid
}

function Get-BundleId {
    param([string]$AppPath)
    $plist = Join-Path $AppPath "Contents/Info.plist"
    $bundleId = (& /usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' $plist 2>$null).Trim()
    if (-not $bundleId) {
        Write-Host "Fehler: Bundle-ID konnte aus $plist nicht gelesen werden." -ForegroundColor Red
        exit 1
    }
    return $bundleId
}

function Assert-PairToMacAvailable {
    if ($isMacOS) { return }
    if (-not $ServerAddress) {
        Write-Host "Fehler: Auf Windows wird ein Pair-to-Mac Build-Host benoetigt." -ForegroundColor Red
        Write-Host "Setze -ServerAddress / -ServerUser / -ServerPassword oder die Umgebungsvariablen:" -ForegroundColor Yellow
        Write-Host "VIDEOPLAYER_IOS_MAC_SERVER_ADDRESS, VIDEOPLAYER_IOS_MAC_SERVER_USER, VIDEOPLAYER_IOS_MAC_SERVER_PASSWORD" -ForegroundColor Yellow
        exit 1
    }
    if ($ServerUser -match '\s') {
        Write-Host "Warnung: -ServerUser enthaelt ein Leerzeichen." -ForegroundColor Yellow
        Write-Host "Pair-to-Mac erwartet den macOS-Kurznamen (z. B. 'martin'), nicht den vollstaendigen Namen." -ForegroundColor Yellow
        Write-Host "Wenn der Pfad _DotNetRootRemoteDirectory ($DotNetRootRemoteDirectory) falsch ist, setze VIDEOPLAYER_IOS_MAC_DOTNET_ROOT." -ForegroundColor Yellow
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
    return Select-FromList -Title $Title -Options $Options -Default $Default
}

function Get-DeployPrefsPath { return (Join-Path $repoRoot ".ios-deploy.user.json") }

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
    if ($Action -eq "simulator" -or $Action -eq "list" -or $Action -eq "menu") { return }
    if ($NoPrompt) { return }
    if (-not $isMacOS -and -not ($ServerAddress -and $ServerUser)) { return }

    $isStore = $Action -in @('store', 'upload')
    $isDevice = $Action -eq 'device'

    # Ein fuer die Aktion unpassender vorgegebener Schluessel (z. B. Distribution bei 'device')
    # wird verworfen und stattdessen interaktiv gewaehlt.
    if ($isDevice -and $CodesignKey -like 'Apple Distribution:*') {
        Write-Host "Hinweis: '$CodesignKey' kann nicht auf Geraeten installiert werden - Auswahl wird angeboten." -ForegroundColor Yellow
        $script:CodesignKey = ""
        $script:CodesignProvision = ""
    }

    $prefs = Get-DeployPrefs
    $saved = if ($prefs -and ($prefs.PSObject.Properties.Name -contains $Action)) { $prefs.$Action } else { $null }

    if ($isDevice -and -not $Device) {
        $devs = @(Get-RemoteDevices)
        if ($devs.Count -eq 0) {
            Write-Host "Warnung: Keine angebundenen Geraete auf dem Mac gefunden (devicectl)." -ForegroundColor Yellow
        }
        else {
            $options = @($devs | ForEach-Object { "$($_.Name) ($($_.Identifier)) [$($_.State)]" })
            $default = $null
            if ($saved -and $saved.Device) {
                for ($k = 0; $k -lt $devs.Count; $k++) {
                    if ($devs[$k].Identifier -eq $saved.Device) { $default = $options[$k]; break }
                }
            }
            if (-not $default) { $default = $options[0] }
            $choice = Select-FromList -Title "Geraet waehlen:" -Options $options -Default $default
            if ($choice) { $script:Device = $devs[$options.IndexOf($choice)].Identifier }
        }
        if (-not $Device) { return }
    }
    if ($CodesignKey -and $CodesignProvision) {
        if ($isDevice -or -not $saved) { Save-DeployPrefs -Action $Action }
        return
    }

    if (-not $CodesignKey) {
        $ids = @(Get-RemoteIdentities)
        if ($isStore) { $ids = @($ids | Where-Object { $_ -like 'Apple Distribution:*' }) }
        elseif ($isDevice) { $ids = @($ids | Where-Object { $_ -like 'Apple Development:*' }) }
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
        elseif ($isDevice) { $profs = @($profs | Where-Object { $_.DevProfile }) }
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
        Write-Host "Fehler: -CodesignKey bzw. VIDEOPLAYER_IOS_CODESIGN_KEY ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
    if (-not $CodesignProvision) {
        Write-Host "Fehler: -CodesignProvision bzw. VIDEOPLAYER_IOS_PROVISIONING_PROFILE ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
}

$script:TranscriptStarted = $false

function Start-DeployLog {
    if ($script:TranscriptStarted) { return }
    try {
        $dir = if ([IO.Path]::IsPathRooted($LogDir)) { $LogDir } else { Join-Path $repoRoot $LogDir }
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
        $logPath = Join-Path $dir "ios-deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
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
    if (-not $CodesignKey) { $missing += "-CodesignKey / VIDEOPLAYER_IOS_CODESIGN_KEY" }
    if (-not $CodesignProvision) { $missing += "-CodesignProvision / VIDEOPLAYER_IOS_PROVISIONING_PROFILE" }
    if (-not $ApiKeyPath) { $missing += "-ApiKeyPath / VIDEOPLAYER_IOS_API_KEY_PATH" }
    if (-not $ApiKeyId) { $missing += "-ApiKeyId / VIDEOPLAYER_IOS_API_KEY_ID" }
    if (-not $ApiIssuerId) { $missing += "-ApiIssuerId / VIDEOPLAYER_IOS_API_ISSUER_ID" }
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
        Write-Host "Setze -ServerAddress / -ServerUser bzw. VIDEOPLAYER_IOS_MAC_SERVER_ADDRESS / VIDEOPLAYER_IOS_MAC_SERVER_USER." -ForegroundColor Yellow
        Write-Host "Voraussetzung: schluesselbasiertes SSH (kein Passwort-Prompt)." -ForegroundColor Yellow
        exit 1
    }
}

function Assert-TransporterAvailable {
    $check = 'ITMS="/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter"; [ -x "$ITMS" ] || xcrun -f iTMSTransporter >/dev/null 2>&1'
    $code = Invoke-OnMac -Script $check -Description "iTMSTransporter-Verfuegbarkeit" -ReturnExitCode
    if ($code -ne 0) {
        Write-Host "Fehler: iTMSTransporter auf dem Mac nicht gefunden." -ForegroundColor Red
        Write-Host "Seit Xcode 16 gehoert es zur Transporter-App - aus dem Mac App Store installieren:" -ForegroundColor Yellow
        Write-Host "https://apps.apple.com/us/app/transporter/id1450874784" -ForegroundColor Yellow
        exit 1
    }
}

function Update-BuildNumber {
    $csprojPath = $projectPath
    $content = [System.IO.File]::ReadAllText($csprojPath)
    $m = [regex]::Match($content, '<ApplicationVersion>(\d+)</ApplicationVersion>')
    if (-not $m.Success) {
        Write-Host "Warnung: <ApplicationVersion> nicht in $csprojPath gefunden - kein Buildnummer-Bump." -ForegroundColor Yellow
        return
    }
    $old = [int]$m.Groups[1].Value
    $new = $old + 1
    $content = $content.Replace($m.Value, "<ApplicationVersion>$new</ApplicationVersion>")
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($csprojPath, $content, $utf8NoBom)
    Write-Host "Buildnummer erhoeht (CFBundleVersion): $old -> $new" -ForegroundColor Green
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
    $dir = Join-Path $repoRoot "VideoPlayer.Maui/bin/$config/$framework/$rid"
    $ipas = Get-ChildItem -Path $dir -Recurse -Filter "*.ipa" -ErrorAction SilentlyContinue
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

function Invoke-OnMac {
    param(
        [string]$Script,
        [string]$Description,
        [switch]$ReturnExitCode
    )
    # CRLF aus den Here-Strings wuerde das Remote-Bash brechen
    # (set -e\r, trap EXIT\r, \r in Pfaden) - auf LF normalisieren.
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
    $remote = '$HOME/ios-uploads/' + $name
    Invoke-OnMac -Script 'mkdir -p "$HOME/ios-uploads"' -Description "Anlegen des Upload-Verzeichnisses"
    Write-Host "Kopiere IPA auf den Mac: $remote" -ForegroundColor Cyan
    Copy-FileToMac -LocalPath $LocalIpa -RemotePath $remote
    return $remote
}

function Invoke-IpaValidation {
    param([string]$MacIpa)
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
# Invarianten der Store-Konfiguration: iPhone-Support und keine
# nicht-freigestellte Verschluesselung. iPad (2) ist fuer VideoPlayer
# explizit erlaubt - die App ist fuer iPhone UND iPad deklariert.
FAMILY=`$(plutil -extract UIDeviceFamily json -o - "`$APP/Info.plist" 2>/dev/null || echo '[]')
echo "`$FAMILY" | grep -q '1' || { echo "FEHLER: UIDeviceFamily ohne iPhone (1): `$FAMILY"; exit 1; }
ENC=`$(plutil -extract ITSAppUsesNonExemptEncryption raw -o - "`$APP/Info.plist" 2>/dev/null || echo '?')
[ "`$ENC" = "false" ] || [ "`$ENC" = "0" ] || { echo "FEHLER: ITSAppUsesNonExemptEncryption nicht false: `$ENC"; exit 1; }
echo "==> Pruefe App-Icon-Kodierung"
# ITMS-90717 lehnt Icons mit Transparenz ab. Der Resizetizer erzeugt
# grundsaetzlich RGBA; actool kodiert ARGB - das ist bei vollstaendig
# opaken Pixeln (Quell-Icon mit opakem Hintergrund) der Normalzustand
# und wird von Apple toleriert. Nur eine Warnung ausgeben.
ICON_ENC=`$(xcrun assetutil --info "`$APP/Assets.car" 2>/dev/null | grep -A8 'appiconItunesArtwork' | grep -i '"Encoding"' | head -1 || true)
if [ -n "`$ICON_ENC" ]; then
    echo "Marketing-Icon-Encoding:`$ICON_ENC"
else
    echo "WARNUNG: Assets.car/appicon konnte nicht gelesen werden - Icon-Check uebersprungen"
fi
echo "==> iTMSTransporter -m verify"
# altool ist bei Apple deprecated; iTMSTransporter nutzt dieselbe
# API-Key-Authentifizierung und denselben Schluesselsuchpfad
# ~/.appstoreconnect/private_keys. Seit Xcode 16 liefert Xcode das
# Werkzeug nicht mehr mit: es gehoert zur Transporter-App aus dem
# Mac App Store und liegt dann unter
# /Applications/Transporter.app/Contents/itms/bin/iTMSTransporter.
# xcrun -f dient als Fallback fuer aeltere Xcode-Versionen. Fuer Apps
# (.ipa/.pkg) verlangt iTMSTransporter -assetFile; -f gilt nur fuer
# .itmsp-Pakete (Apple Transporter User Guide). Falls das Werkzeug
# fehlt oder -m verify fuer iOS-IPAs nicht unterstuetzt wird,
# entfaellt die Remote-Validierung: die lokale codesign-Pruefung oben
# ist bestanden und der Upload validiert serverseitig
# (dokumentierter Fallback).
ITMS="/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter"
if [ ! -x "`$ITMS" ]; then ITMS=`$(xcrun -f iTMSTransporter 2>/dev/null || true); fi
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
    $macScript = @"
set -e
# -assetFile statt -f: -f ist fuer .itmsp-Pakete reserviert und darf fuer
# App-Uploads nicht verwendet werden (Apple Transporter User Guide).
# iTMSTransporter wird seit Xcode 16 nicht mehr mit Xcode geliefert:
# es gehoert zur Transporter-App aus dem Mac App Store.
ITMS="/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter"
if [ ! -x "`$ITMS" ]; then ITMS=`$(xcrun -f iTMSTransporter 2>/dev/null || true); fi
if [ -z "`$ITMS" ] || [ ! -x "`$ITMS" ]; then
    echo "FEHLER: iTMSTransporter nicht gefunden - Transporter-App aus dem Mac App Store installieren."
    exit 1
fi
"`$ITMS" -m upload -assetFile "$MacIpa" -apiKey "$ApiKeyId" -apiIssuer "$ApiIssuerId"
echo "==> Upload erfolgreich - der Build erscheint nach der Verarbeitung in App Store Connect / TestFlight"
"@
    Invoke-OnMac -Script $macScript -Description "App-Store-Upload"
}

function Invoke-Store {
    Assert-StorePrerequisites -Action "store"
    Assert-CodesigningForAction -Action "store"
    Assert-TransporterAvailable
    if (-not $NoBumpBuildNumber) { Update-BuildNumber }
    Invoke-Build
    $ipa = Get-LatestIpa
    Write-Host "IPA: $ipa" -ForegroundColor Green
    Copy-ApiKeyToMac | Out-Null
    $macIpa = Get-MacIpaPath -LocalIpa $ipa
    Invoke-IpaValidation -MacIpa $macIpa
    Invoke-StoreUpload -MacIpa $macIpa
    Write-Host "Store-Lauf abgeschlossen." -ForegroundColor Green
}

function Invoke-Upload {
    Assert-StorePrerequisites -Action "upload"
    Assert-CodesigningForAction -Action "upload"
    Assert-TransporterAvailable
    $ipa = Get-LatestIpa
    Write-Host "IPA: $ipa" -ForegroundColor Green
    Copy-ApiKeyToMac | Out-Null
    $macIpa = Get-MacIpaPath -LocalIpa $ipa
    Invoke-IpaValidation -MacIpa $macIpa
    Invoke-StoreUpload -MacIpa $macIpa
    Write-Host "Upload abgeschlossen." -ForegroundColor Green
}

function Invoke-Build {
    $rid = Get-RuntimeIdentifier -Action "build"
    $config = Get-Configuration -Action "build"
    $hasCodesigning = $CodesignKey -and $CodesignProvision

    $rspLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $rspLines -Name "RuntimeIdentifier" -Value $rid
    Add-PropertyLine -List $rspLines -Name "VideoPlayerApiToken" -Value $VideoPlayerApiToken

    if ($isWindows) {
        Add-PropertyLine -List $rspLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $rspLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $rspLines -Name "TcpPort" -Value $TcpPort
        Add-PropertyLine -List $rspLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $rspLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    $displayLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $displayLines -Name "RuntimeIdentifier" -Value $rid
    Add-MaskedDisplay -List $displayLines -Name "VideoPlayerApiToken" -Value $VideoPlayerApiToken
    if ($isWindows) {
        Add-PropertyLine -List $displayLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $displayLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $displayLines -Name "TcpPort" -Value $TcpPort
        Add-MaskedDisplay -List $displayLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $displayLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    if ($hasCodesigning) {
        $verb = "publish"
        Add-PropertyLine -List $rspLines -Name "ArchiveOnBuild" -Value "true"
        Add-PropertyLine -List $rspLines -Name "CodesignKey" -Value $CodesignKey
        Add-PropertyLine -List $rspLines -Name "CodesignProvision" -Value $CodesignProvision
        Add-PropertyLine -List $rspLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        Add-PropertyLine -List $displayLines -Name "ArchiveOnBuild" -Value "true"
        Add-PropertyLine -List $displayLines -Name "CodesignKey" -Value $CodesignKey
        Add-PropertyLine -List $displayLines -Name "CodesignProvision" -Value $CodesignProvision
        Add-PropertyLine -List $displayLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        Write-Host "Erzeuge signiertes iOS-Kompilat (.ipa)..." -ForegroundColor Cyan
    }
    else {
        $verb = "build"
        Write-Host "Baue iOS-App ohne Codesigning..." -ForegroundColor Cyan
    }

    $rspPath = New-ResponseFile -Lines $rspLines -Directory $repoRoot
    try {
        Write-Host "dotnet $verb $projectPath -f $framework -c $config @$rspPath" -ForegroundColor Gray
        $baseArgs = @($verb, $projectPath, "-f", $framework, "-c", $config, "@$rspPath")
        $displayArgs = @($verb, $projectPath, "-f", $framework, "-c", $config) + $displayLines
        Write-Host "Eigenschaften:" -ForegroundColor Gray
        foreach ($line in $displayLines) { Write-Host "   $line" -ForegroundColor Gray }

        & dotnet @baseArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build fehlgeschlagen (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
            Write-Host "Response-Datei (maskiert):" -ForegroundColor Gray
            Write-MaskedPropertyLines -Lines $rspLines
            exit 1
        }

        if ($hasCodesigning) {
            $ipa = Get-ChildItem -Path (Join-Path $repoRoot "VideoPlayer.Maui/bin/$config/$framework/$rid") -Recurse -Filter "*.ipa" | Select-Object -First 1
            if (-not $ipa) {
                Write-Host "Keine IPA-Datei gefunden." -ForegroundColor Red
                exit 1
            }
            Write-Host "IPA gefunden: $($ipa.FullName)" -ForegroundColor Green
        }
        else {
            Write-Host "Build erfolgreich (ohne Codesigning, keine .ipa)." -ForegroundColor Green
        }
    }
    finally {
        Remove-Item -Path $rspPath -ErrorAction SilentlyContinue
    }
}

function Invoke-SimulatorMac {
    $rid = Get-RuntimeIdentifier -Action "simulator"
    $config = Get-Configuration -Action "simulator"
    $udid = Get-SimulatorUdid
    $appPath = Join-Path $repoRoot "VideoPlayer.Maui/bin/$config/$framework/$rid/VideoPlayer.Maui.app"
    $screenshotDir = Join-Path $repoRoot "VideoPlayer.Maui/bin/$config/$framework/$rid"
    $screenshotPath = Join-Path $screenshotDir "simulator-screenshot-$(Get-Date -Format 'yyyyMMdd-HHmmss').png"

    if (-not (Test-Path $appPath)) {
        Write-Host "Baue iOS-Simulator-App ..." -ForegroundColor Cyan
        $buildArgs = @("build", $projectPath, "-f", $framework, "-c", $config, "-p:RuntimeIdentifier=$rid")
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build fehlgeschlagen." -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host "Vorhandene App wird verwendet: $appPath" -ForegroundColor Gray
    }

    Write-Host "Boote Simulator ($udid) ..." -ForegroundColor Cyan
    $bootOutput = & xcrun simctl boot $udid 2>&1
    if ($LASTEXITCODE -ne 0 -and $bootOutput -notmatch 'already booted') {
        Write-Host "Fehler beim Booten des Simulators: $bootOutput" -ForegroundColor Red
        exit 1
    }

    $bundleId = Get-BundleId -AppPath $appPath
    Write-Host "Bundle-ID: $bundleId" -ForegroundColor Gray

    Write-Host "Installiere App im Simulator ..." -ForegroundColor Cyan
    $installOutput = & xcrun simctl install $udid $appPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        if ($installOutput -match 'already installed') {
            Write-Host "App bereits installiert, deinstalliere und installiere neu ..." -ForegroundColor Yellow
            & xcrun simctl uninstall $udid $bundleId 2>&1 | Out-Null
            & xcrun simctl install $udid $appPath
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Fehler: Installation fehlgeschlagen." -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "Fehler bei der Installation: $installOutput" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host "Starte App im Simulator ..." -ForegroundColor Cyan
    & xcrun simctl launch $udid $bundleId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler beim Starten der App." -ForegroundColor Red
        exit 1
    }

    Write-Host "Warte 5 Sekunden auf Rendering ..." -ForegroundColor Gray
    Start-Sleep -Seconds 5

    Write-Host "Erstelle Screenshot ..." -ForegroundColor Cyan
    & xcrun simctl io $udid screenshot $screenshotPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler beim Erstellen des Screenshots." -ForegroundColor Red
        exit 1
    }

    Write-Host "Screenshot gespeichert: $screenshotPath" -ForegroundColor Green
    & open $screenshotPath
}

function Invoke-Run {
    param([string]$Action)
    if ($isWindows) {
        Write-Host "Fehler: iOS-Simulator-/Geraete-Deployment via 'dotnet build -t:Run' wird von Microsoft auf Windows nicht unterstuetzt." -ForegroundColor Red
        Write-Host "Moeglichkeiten:" -ForegroundColor Yellow
        Write-Host "  1) Skript auf dem Mac ausfuehren: ./scripts/iOS-Deployment.ps1 -Action $Action" -ForegroundColor Yellow
        Write-Host "  2) Visual Studio verwenden." -ForegroundColor Yellow
        Write-Host "  3) Zuerst 'build' ausfuehren und die .app/.ipa manuell auf dem Mac deployen." -ForegroundColor Yellow
        exit 1
    }
    if ($isMacOS -and $Action -eq "simulator") {
        Invoke-SimulatorMac
        return
    }
    $rid = Get-RuntimeIdentifier -Action $Action
    $config = Get-Configuration -Action $Action

    $rspLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $rspLines -Name "RuntimeIdentifier" -Value $rid
    Add-PropertyLine -List $rspLines -Name "VideoPlayerApiToken" -Value $VideoPlayerApiToken

    if ($isWindows) {
        Add-PropertyLine -List $rspLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $rspLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $rspLines -Name "TcpPort" -Value $TcpPort
        Add-PropertyLine -List $rspLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $rspLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    $displayLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $displayLines -Name "RuntimeIdentifier" -Value $rid
    Add-MaskedDisplay -List $displayLines -Name "VideoPlayerApiToken" -Value $VideoPlayerApiToken
    if ($isWindows) {
        Add-PropertyLine -List $displayLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $displayLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $displayLines -Name "TcpPort" -Value $TcpPort
        Add-MaskedDisplay -List $displayLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $displayLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    if ($Action -eq "simulator") {
        if ($Device) {
            $udid = $Device
            if (-not $udid.StartsWith(":v2:udid=")) { $udid = ":v2:udid=$udid" }
            Add-PropertyLine -List $rspLines -Name "_DeviceName" -Value $udid
            Add-PropertyLine -List $displayLines -Name "_DeviceName" -Value $udid
        }
    }
    elseif ($Action -eq "device") {
        if (-not $Device) {
            Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID) angegeben werden." -ForegroundColor Red
            exit 1
        }
        Add-PropertyLine -List $rspLines -Name "_DeviceName" -Value $Device
        Add-PropertyLine -List $displayLines -Name "_DeviceName" -Value $Device
        if ($CodesignKey) {
            Add-PropertyLine -List $rspLines -Name "CodesignKey" -Value $CodesignKey
            Add-PropertyLine -List $displayLines -Name "CodesignKey" -Value $CodesignKey
        }
        if ($CodesignProvision) {
            Add-PropertyLine -List $rspLines -Name "CodesignProvision" -Value $CodesignProvision
            Add-PropertyLine -List $displayLines -Name "CodesignProvision" -Value $CodesignProvision
        }
        if ($CodesignEntitlements) {
            Add-PropertyLine -List $rspLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
            Add-PropertyLine -List $displayLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        }
    }

    $rspPath = New-ResponseFile -Lines $rspLines -Directory $repoRoot
    try {
        Write-Host "Starte iOS $Action ..." -ForegroundColor Cyan
        $baseArgs = @("build", $projectPath, "-t:Run", "-f", $framework, "-c", $config, "@$rspPath")
        Write-Host "dotnet build $projectPath -t:Run -f $framework -c $config @$rspPath" -ForegroundColor Gray
        Write-Host "Eigenschaften:" -ForegroundColor Gray
        foreach ($line in $displayLines) { Write-Host "   $line" -ForegroundColor Gray }

        & dotnet @baseArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Host "$Action fehlgeschlagen (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
            Write-Host "Response-Datei (maskiert):" -ForegroundColor Gray
            Write-MaskedPropertyLines -Lines $rspLines
            exit 1
        }
        Write-Host "iOS $Action erfolgreich." -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $rspPath -ErrorAction SilentlyContinue
    }
}

function Get-LocalBundleId {
    $plist = Join-Path $repoRoot "VideoPlayer.Maui/Platforms/iOS/Info.plist"
    $m = [regex]::Match([IO.File]::ReadAllText($plist), '<key>CFBundleIdentifier</key>\s*<string>([^<]+)</string>')
    if (-not $m.Success) {
        Write-Host "Fehler: CFBundleIdentifier konnte nicht aus $plist gelesen werden." -ForegroundColor Red
        exit 1
    }
    return $m.Groups[1].Value
}

function Invoke-DeviceViaSsh {
    if (-not $Device) {
        Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID) angegeben werden." -ForegroundColor Red
        Write-Host "UDID per SSH anzeigen: -Action list" -ForegroundColor Yellow
        exit 1
    }
    $rid = "ios-arm64"
    $config = Get-Configuration -Action "device"

    $rspLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $rspLines -Name "RuntimeIdentifier" -Value $rid
    Add-PropertyLine -List $rspLines -Name "VideoPlayerApiToken" -Value $VideoPlayerApiToken
    Add-PropertyLine -List $rspLines -Name "ServerAddress" -Value $ServerAddress
    Add-PropertyLine -List $rspLines -Name "ServerUser" -Value $ServerUser
    Add-PropertyLine -List $rspLines -Name "TcpPort" -Value $TcpPort
    Add-PropertyLine -List $rspLines -Name "ServerPassword" -Value $ServerPassword
    Add-PropertyLine -List $rspLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    Add-PropertyLine -List $rspLines -Name "CodesignKey" -Value $CodesignKey
    Add-PropertyLine -List $rspLines -Name "CodesignProvision" -Value $CodesignProvision

    $rspPath = New-ResponseFile -Lines $rspLines -Directory $repoRoot
    try {
        Write-Host "Baue iOS-App via Pair-to-Mac ($config/$rid) ..." -ForegroundColor Cyan
        & dotnet publish $projectPath -f $framework -c $config "@$rspPath"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build fehlgeschlagen (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
            Write-Host "Response-Datei (maskiert):" -ForegroundColor Gray
            Write-MaskedPropertyLines -Lines $rspLines
            exit 1
        }
    }
    finally {
        Remove-Item -Path $rspPath -ErrorAction SilentlyContinue
    }

    # Die .ipa enthaelt die signierte .app und wird zuverlaessig nach Windows zurueckkopiert;
    # das lokale bin/.../VideoPlayer.Maui.app bleibt bei Pair-to-Mac-Builds leer.
    $publishDir = Join-Path $repoRoot "VideoPlayer.Maui/bin/$config/$framework/$rid/publish"
    $ipa = Get-ChildItem -Path $publishDir -Filter "*.ipa" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $ipa) {
        Write-Host "Fehler: Keine .ipa unter $publishDir gefunden." -ForegroundColor Red
        exit 1
    }
    Write-Host "IPA: $($ipa.FullName)" -ForegroundColor Cyan

    Invoke-OnMac -Script 'mkdir -p "$HOME/ios-uploads"' -Description "Anlegen des Upload-Verzeichnisses"
    Write-Host "Kopiere IPA auf den Mac ..." -ForegroundColor Cyan
    Copy-FileToMac -LocalPath $ipa.FullName -RemotePath "`$HOME/ios-uploads/$($ipa.Name)"

    Write-Host "Installiere auf Geraet $Device ..." -ForegroundColor Cyan
    $installScript = @"
set -e
TMP=`$(mktemp -d)
trap 'rm -rf "`$TMP"' EXIT
ditto -x -k "`$HOME/ios-uploads/$($ipa.Name)" "`$TMP" 2>/dev/null || unzip -q "`$HOME/ios-uploads/$($ipa.Name)" -d "`$TMP"
APP=`$(find "`$TMP/Payload" -maxdepth 1 -name '*.app' | head -1)
if [ -z "`$APP" ]; then echo "Kein .app-Bundle in der IPA gefunden"; exit 1; fi
xcrun devicectl device install app --device "$Device" "`$APP"
"@
    Invoke-OnMac -Script $installScript -Description "Installation auf dem Geraet"

    $bundleId = Get-LocalBundleId
    if ($Console) {
        Write-Host "Starte App mit Console-Streaming (Ctrl+C loest die Verbindung, die App laeuft weiter) ..." -ForegroundColor Cyan
        Invoke-OnMac -Script "xcrun devicectl device process launch --device `"$Device`" --console $bundleId" -Description "App-Start mit Console" -ReturnExitCode | Out-Null
        return
    }
    Invoke-OnMac -Script "xcrun devicectl device process launch --device `"$Device`" $bundleId" -Description "App-Start auf dem Geraet"
    Write-Host "App gestartet. Tipp: -Console streamt die App-Ausgabe (inkl. Crash-Details) ins Terminal." -ForegroundColor Green
}

function Invoke-List {
    if ($isMacOS) {
        Write-Host "Verfuegbare iOS-Simulatoren:" -ForegroundColor Cyan
        & xcrun simctl list devices
    }
    elseif ($ServerAddress -and $ServerUser) {
        Write-Host "Geraete auf $ServerAddress (via SSH):" -ForegroundColor Cyan
        Invoke-OnMac -Script 'xcrun devicectl list devices; echo "--- Simulatoren ---"; xcrun simctl list devices available' -Description "Geraete auflisten"
    }
    else {
        Write-Host "Auflistung von Simulatoren/Geraeten ist nur auf macOS verfuegbar." -ForegroundColor Yellow
        Write-Host "Auf Windows: -ServerAddress/-ServerUser setzen, dann fragt 'list' den Mac per SSH ab." -ForegroundColor Yellow
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "==============================="
    Write-Host "   iOS Build & Deployment Menue"
    Write-Host "==============================="
    Write-Host "1) Build (iOS-App, optional .ipa mit Codesigning)"
    Write-Host "2) Build + iOS-Simulator starten"
    Write-Host "3) Build + echtes Geraet deployen"
    Write-Host "4) Simulatoren/Geraete anzeigen"
    Write-Host "5) Release-Build + Upload zu App Store Connect (TestFlight)"
    Write-Host "6) Vorhandene .ipa validieren + hochladen"
    Write-Host "==============================="
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
            Assert-PairToMacAvailable
            Invoke-Build
        }
        "simulator" {
            Assert-PairToMacAvailable
            Invoke-Run -Action "simulator"
        }
        "device" {
            if ($isWindows -and $ServerAddress -and $ServerUser) {
                Assert-CodesigningForAction -Action "device"
                if (-not $Device) {
                    Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID oder Geraetename) angegeben werden." -ForegroundColor Red
                    Write-Host "Geraete anzeigen: -Action list" -ForegroundColor Yellow
                    exit 1
                }
                Invoke-DeviceViaSsh
            }
            else {
                Assert-PairToMacAvailable
                Assert-CodesigningForAction -Action "device"
                Invoke-Run -Action "device"
            }
        }
        "store" {
            Assert-PairToMacAvailable
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
