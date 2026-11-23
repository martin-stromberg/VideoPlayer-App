using Foundation;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Löst die Verzeichniswurzeln der lokalen Datenablage unter tvOS auf und
    /// legt sie an. tvOS kennt kein Backup-Konzept (kein iCloud-/Geräte-Backup,
    /// kein <c>NoBackupFilesDir</c>): <c>Library/Caches</c> ist der einzige
    /// größere Ablageort und kann vom System bei Speichermangel geräumt
    /// werden — eine Räumung erzwingt die Neu-Einrichtung der App. Die drei
    /// Wurzeln des <see cref="AppStorageLayout"/> bleiben dennoch getrennt,
    /// damit die Layout-Semantik erhalten bleibt.
    /// </summary>
    public static class TvosStorageLayoutProvider
    {
        /// <summary>
        /// Erstellt das Ablage-Layout für das aktive Profil und legt die
        /// Verzeichnisse an (unter <c>Library/Caches/videoplayer</c>).
        /// </summary>
        /// <param name="profile">Aktives Anwendungsprofil (steuert die Unterverzeichnis-Namen).</param>
        /// <returns>Das initialisierte Ablage-Layout mit angelegten Verzeichnissen.</returns>
        public static AppStorageLayout CreateLayout(AppProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var layout = new AppStorageLayout(
                backupRoot: CachesPath("videoplayer/data"),
                noBackupRoot: CachesPath("videoplayer/nobackup"),
                cacheRoot: CachesPath("videoplayer/cache"),
                profile: profile);

            layout.EnsureDirectoriesExist();
            return layout;
        }

        private static string CachesPath(string relative)
        {
            var caches = NSFileManager.DefaultManager.GetUrls(
                NSSearchPathDirectory.CachesDirectory,
                NSSearchPathDomain.User)[0];
            return System.IO.Path.Combine(caches.Path!, relative);
        }
    }
}
