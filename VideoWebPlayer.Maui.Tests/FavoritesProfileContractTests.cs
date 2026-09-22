using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Favoriten- und Profil-Seite (Schritt 9): die
/// MAUI-Schicht kann im Testprojekt nicht instanziiert werden —
/// verdrahtete Struktur und Zusammenspiel werden daher über die
/// Quelldateien geprüft (Muster: <see cref="SourcesContractTests"/>).
/// Echte UI-End-to-End-Automatisierung ist im Repository derzeit nicht
/// vorhanden (dokumentierte Einschränkung).
/// </summary>
public class FavoritesProfileContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft FavoritesPage: zeigt Favoriten mit Poster, Titel, Typ und Entfernen-Aktion samt Offline-Hinweis.</summary>
    [Fact]
    public void FavoritesPage_ShowsItems_WithRemove_AndOfflineHint()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "FavoritesPage.xaml");

        // Liste an den Seitenzustand gebunden; Einträge mit Poster
        // (Cache-Bild), Titel und Typ-Anzeige wie in der Webanwendung.
        Assert.Contains("{Binding Model.FavoritesPage.Items}", xaml, StringComparison.Ordinal);
        Assert.Contains("CachedPosterImage", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding PosterPictureId}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Title}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding MediaTypeText}", xaml, StringComparison.Ordinal);
        // Direkte Entfernen-Aktion je Eintrag; offline gesperrt.
        Assert.Contains("RemoveButton", xaml, StringComparison.Ordinal);
        Assert.Contains("Entfernen", xaml, StringComparison.Ordinal);
        Assert.Contains("Model.IsOffline", xaml, StringComparison.Ordinal);
        // Offline-Banner, Lade-/Fehler-/Leerzustand.
        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.FavoritesPage.IsLoading}", xaml, StringComparison.Ordinal);
        Assert.Contains("RetryButton", xaml, StringComparison.Ordinal);
        Assert.Contains("EmptyLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft FavoritesPage: dispatcht Öffnen-, Entfernen- und Auswahl-Nachrichten.</summary>
    [Fact]
    public void FavoritesPage_DispatchesOpenRemoveAndSelection()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "FavoritesPage.xaml.cs");

        Assert.Contains("FavoritesPageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("FavoriteRemoveRequestedMessage", code, StringComparison.Ordinal);
        // Die Auswahl navigiert über die gemeinsame Detail-Nachricht.
        Assert.Contains("MediaItemSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("ToMediaItem", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ProfilePage: zeigt Benutzer, lokales Profil und den Abmelden-Schalter.</summary>
    [Fact]
    public void ProfilePage_ShowsUserInfo_Profile_AndLogout()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "ProfilePage.xaml");

        Assert.Contains("{Binding Model.Profile.UsernameText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.Profile.EmailText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.Profile.ProfileNameText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.ActiveServerAddress}", xaml, StringComparison.Ordinal);
        Assert.Contains("LogoutButton", xaml, StringComparison.Ordinal);
        Assert.Contains("Abmelden", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ProfilePage: dispatcht die Öffnen- und Abmelden-Nachrichten.</summary>
    [Fact]
    public void ProfilePage_DispatchesOpenAndLogout()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "ProfilePage.xaml.cs");

        Assert.Contains("ProfilePageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("LogoutRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: führt die neuen Effekte aus und meldet die Cache-Änderung nach dem Entfernen.</summary>
    [Fact]
    public void EffectHandler_HandlesStep9Effects_AndRefreshesCachesAfterRemove()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadFavoritesPageEffect", handler, StringComparison.Ordinal);
        Assert.Contains("RemoveFavoriteEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadProfileInfoEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LogoutEffect", handler, StringComparison.Ordinal);
        // Online wird die lokale Kopie zuerst vom Serverstand erneuert.
        Assert.Contains("SyncFavoritesAsync", handler, StringComparison.Ordinal);
        Assert.Contains("LoadFavoriteItemsAsync", handler, StringComparison.Ordinal);
        // Nach dem Entfernen laden die Startseiten-Listen den neuen
        // Cache-Stand ohne manuelle Aktualisierung nach.
        Assert.Contains("CachedListsChangedMessage", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert den Profil-Info-Dienst.</summary>
    [Fact]
    public void MauiProgram_RegistersProfileInfoService()
    {
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("ProfileInfoService", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: Favoriten und Profil bleiben Menüeinträge.</summary>
    [Fact]
    public void AppShell_KeepsFavoritesAndProfileFlyoutItems()
    {
        var shell = Read("VideoPlayer.Maui", "AppShell.xaml");

        Assert.Contains("FavoritesPage", shell, StringComparison.Ordinal);
        Assert.Contains("ProfilePage", shell, StringComparison.Ordinal);
        Assert.Contains("favorites", shell, StringComparison.Ordinal);
        Assert.Contains("profile", shell, StringComparison.Ordinal);
    }
}
