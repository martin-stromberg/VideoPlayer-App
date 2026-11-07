using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Löst die plattformspezifischen Wurzelverzeichnisse der lokalen Datenablage
/// auf und legt sie an. Backup-Klassifikation:
/// <list type="bullet">
/// <item>Backup-pflichtig (<see cref="AppStorageLayout.BackupDirectory"/>):
/// Windows → Roaming-Profilverzeichnis <c>%APPDATA%\VideoPlayer</c>
/// (Roaming-Äquivalent zum Cloud-Backup; die App ist unpackaged,
/// <c>ApplicationData.RoamingFolder</c> steht daher nicht zur Verfügung);
/// iOS/MacCatalyst/Android → <c>FileSystem.AppDataDirectory/data</c>
/// (unter iOS automatisch Bestandteil des Geräte-/iCloud-Backups).</item>
/// <item>Nicht backup-pflichtig (<see cref="AppStorageLayout.NoBackupDirectory"/>):
/// iOS/MacCatalyst → <c>AppDataDirectory/nobackup</c> mit
/// <c>NSFileManager.SetSkipBackupAttribute</c>; Android →
/// <c>NoBackupFilesDir</c>; Windows → lokale App-Ablage
/// (<c>AppDataDirectory/nobackup</c>, kein Roaming).</item>
/// <item>Temp-Cache (<see cref="AppStorageLayout.TempDirectory"/>):
/// <c>FileSystem.CacheDirectory</c> — plattformseitig nie gesichert.</item>
/// </list>
/// </summary>
public static class AppStorageLayoutProvider
{
    /// <summary>
    /// Erstellt das Ablage-Layout für das aktive Profil, legt die Verzeichnisse
    /// an und wendet den plattformseitigen Backup-Ausschluss an.
    /// </summary>
    /// <param name="profile">Aktives Anwendungsprofil (steuert die Unterverzeichnis-Namen).</param>
    /// <returns>Das initialisierte Ablage-Layout mit angelegten Verzeichnissen.</returns>
    public static AppStorageLayout CreateLayout(AppProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var layout = new AppStorageLayout(
            backupRoot: GetBackupRoot(),
            noBackupRoot: GetNoBackupRoot(),
            cacheRoot: FileSystem.CacheDirectory,
            profile: profile);

        layout.EnsureDirectoriesExist();
        ApplyBackupExclusion(layout);
        return layout;
    }

    private static string GetBackupRoot()
    {
#if WINDOWS
        // Roaming-Äquivalent: servergespeichertes Profilverzeichnis.
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoPlayer");
#else
        return Path.Combine(FileSystem.AppDataDirectory, "data");
#endif
    }

    private static string GetNoBackupRoot()
    {
#if ANDROID
        var noBackupDir = Android.App.Application.Context.NoBackupFilesDir?.AbsolutePath;
        if (!string.IsNullOrEmpty(noBackupDir))
        {
            return noBackupDir;
        }

        return Path.Combine(FileSystem.AppDataDirectory, "nobackup");
#else
        return Path.Combine(FileSystem.AppDataDirectory, "nobackup");
#endif
    }

    private static void ApplyBackupExclusion(AppStorageLayout layout)
    {
#if IOS || MACCATALYST
        // Plattformüblicher Backup-Ausschluss: das NoBackup-Verzeichnis (und
        // damit Offline-Videos, Vorschaubilder, Cache-Datenbank und Manifest)
        // geht nicht ins iCloud-/Geräte-Backup.
        Foundation.NSFileManager.SetSkipBackupAttribute(layout.NoBackupDirectory, true);
#endif
    }
}
