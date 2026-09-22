using System.Xml.Linq;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests für Einstellungsseite und lokale Datenhaltung der neuen App.
/// Da das Testprojekt keine MAUI-Targets referenzieren kann, werden Struktur
/// und Verdrahtung über die Quelldateien geprüft (Muster:
/// <see cref="MauiSettingsContractTests"/>, <see cref="VideoPlayerMauiContractTests"/>).
/// </summary>
public class SettingsStorageContractTests
{
    private static readonly string Root = RepositoryRoot.Find();
    private static readonly XNamespace MauiNs = "http://schemas.microsoft.com/dotnet/2021/maui";

    /// <summary>AutomationIds der acht Einstellungs-Bedienelemente + Fehlerhinweis.</summary>
    private static readonly string[] ExpectedAutomationIds =
    [
        "AutoDownloadSwitch",
        "PreloadEpisodeCountEntry",
        "WifiOnlySwitch",
        "DownloadQuotaGbEntry",
        "AutoDeleteWatchedSwitch",
        "AutoResumeSwitch",
        "AutoResumeRemainingEntry",
        "WatchedEndWindowEntry",
        "InvalidInputLabel"
    ];

    private static string ReadMaui(params string[] parts)
    {
        return File.ReadAllText(Path.Combine([Root, "VideoPlayer.Maui", .. parts]));
    }

    /// <summary>Prüft SettingsPage: enthält alle Einstellungsteuerelemente.</summary>
    [Fact]
    public void SettingsPage_ContainsAllSettingControls()
    {
        var page = XDocument.Load(
            Path.Combine(Root, "VideoPlayer.Maui", "Pages", "SettingsPage.xaml"));

        var automationIds = page.Descendants()
            .Select(e => (string?)e.Attribute("AutomationId"))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in ExpectedAutomationIds)
        {
            Assert.Contains(id, automationIds);
        }

        // Vier Switches + vier numerische Eingabefelder für die acht Einstellungen.
        Assert.Equal(4, page.Descendants(MauiNs + "Switch").Count());
        Assert.Equal(4, page.Descendants(MauiNs + "Entry").Count());
        Assert.All(page.Descendants(MauiNs + "Entry"), e =>
            Assert.Equal("Numeric", (string?)e.Attribute("Keyboard")));
    }

    /// <summary>Prüft SettingsPage: verdrahtet MVU-Host und Speichern-Nachricht.</summary>
    [Fact]
    public void SettingsPage_WiresMvuHostAndSaveMessage()
    {
        var codeBehind = ReadMaui("Pages", "SettingsPage.xaml.cs");

        Assert.Contains("MvuHost", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SaveSettingsRequestedMessage", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ModelChanged", codeBehind, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Storage-Services.</summary>
    [Fact]
    public void MauiProgram_RegistersStorageServices()
    {
        var mauiProgram = ReadMaui("MauiProgram.cs");

        Assert.Contains("AppProfile.FromEnvironment", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("PreferencesSettingsStore", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("SecureStorageSecretStore", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("AppSettingsService", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("AppSecretsService", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("AppStorageLayoutProvider", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("BackupDatabase", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("CacheDatabase", mauiProgram, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt alle Storage-Effekte.</summary>
    [Fact]
    public void EffectHandler_HandlesAllStorageEffects()
    {
        var handler = ReadMaui("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("InitializeStorageEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadSettingsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PersistSettingsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("SettingsLoadedMessage", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerAddressProvider: verwendet den KnownServersService.</summary>
    [Fact]
    public void ServerAddressProvider_UsesKnownServersService()
    {
        // Schritt 4: Die Serveradresse kommt aus der backup-pflichtigen
        // Serververwaltung (zuletzt verbundener Server); der Legacy-
        // Ablageschlüssel wird über KnownServersService migriert.
        var provider = ReadMaui("Services", "ServerAddressProvider.cs");

        Assert.Contains("KnownServersService", provider, StringComparison.Ordinal);
        Assert.Contains("GetLastConnectedAddressAsync", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences.Default", provider, StringComparison.Ordinal);
    }

    /// <summary>Prüft NoDirectPlatformStorageAccess: kein direkter Zugriff auf Plattform-Storage.</summary>
    [Fact]
    public void NoDirectPlatformStorageAccess()
    {
        // Direkter Zugriff auf Preferences/SecureStorage ist nur in den
        // Adapter-Klassen erlaubt — alle übrigen Konsumenten gehen über die
        // Abstraktionen (Geheimnis-Trennung, Profil-Isolation).
        var mauiDir = Path.Combine(Root, "VideoPlayer.Maui");
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PreferencesSettingsStore.cs",
            "SecureStorageSecretStore.cs",
            "MauiProgram.cs" // DI-Registrierung der Adapter
        };

        foreach (var file in Directory.EnumerateFiles(mauiDir, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                              && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var source = File.ReadAllText(file);
            var name = Path.GetFileName(file);
            if (allowed.Contains(name))
            {
                continue;
            }

            Assert.DoesNotContain("Preferences.Default", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SecureStorage.Default", source, StringComparison.Ordinal);
        }
    }

    /// <summary>Prüft IosBackupExclusion: ist implementiert.</summary>
    [Fact]
    public void IosBackupExclusion_IsImplemented()
    {
        var provider = ReadMaui("Services", "AppStorageLayoutProvider.cs");

        // Plattformüblicher Backup-Ausschluss für das NoBackup-Verzeichnis.
        Assert.Contains("IOS", provider, StringComparison.Ordinal);
        Assert.Contains("SetSkipBackupAttribute", provider, StringComparison.Ordinal);
    }

    /// <summary>Prüft WindowsRoaming: wird für das Backup-Wurzelverzeichnis verwendet.</summary>
    [Fact]
    public void WindowsRoaming_IsUsedForBackupRoot()
    {
        var provider = ReadMaui("Services", "AppStorageLayoutProvider.cs");

        // Roaming-Äquivalent auf Windows: %APPDATA% (SpecialFolder.ApplicationData).
        Assert.Contains("WINDOWS", provider, StringComparison.Ordinal);
        Assert.Contains("SpecialFolder.ApplicationData", provider, StringComparison.Ordinal);
    }

    /// <summary>Prüft StorageProject: wird von allen Projekten referenziert.</summary>
    [Fact]
    public void StorageProject_IsReferencedEverywhere()
    {
        var mvuCsproj = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Mvu", "VideoPlayer.Mvu.csproj"));
        var mauiCsproj = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Maui", "VideoPlayer.Maui.csproj"));
        var testsCsproj = File.ReadAllText(Path.Combine(Root, "VideoWebPlayer.Maui.Tests", "VideoWebPlayer.Maui.Tests.csproj"));

        Assert.Contains("VideoPlayer.Storage.csproj", mvuCsproj, StringComparison.Ordinal);
        Assert.Contains("VideoPlayer.Storage.csproj", mauiCsproj, StringComparison.Ordinal);
        Assert.Contains("VideoPlayer.Storage.csproj", testsCsproj, StringComparison.Ordinal);
    }
}
