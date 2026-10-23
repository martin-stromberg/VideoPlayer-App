using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Hero-Kopfbereich der Startseite (Schritt 5 des Design-Projekts):
/// einmaliges Laden des servergenerierten Hintergrundbilds je Verbindung,
/// Ein-/Ausblenden über die positiv formulierte Einstellung
/// „Kopfbereich anzeigen" (Standard: an) und stabiler Zustand bei
/// fehlgeschlagenem Abruf.
/// </summary>
public class AppUpdateTests_HomeHero
{
    private static AppModel ConnectedModel() => new()
    {
        ConnectionState = ConnectionState.Connected,
        ActiveServerAddress = "http://nas/videoplayer/",
        CurrentPage = AppPage.Home,
        IsOnline = true,
        IsStartupRunning = false
    };

    /// <summary>Prüft LoginCompleted: lädt den Hero-Hintergrund beim Verbindungsaufbau (Einstellung aktiv).</summary>
    [Fact]
    public void LoginCompleted_WithHeroEnabled_LoadsHeroBackground()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = "http://nas/videoplayer/"
        };

        var transition = AppUpdate.Update(model, new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.Contains(transition.Effects, e => e is LoadHeroBackgroundEffect);
        Assert.True(transition.Model.HomeHero.IsLoading);
        Assert.True(transition.Model.ShowHomeHero);
    }

    /// <summary>Prüft LoginCompleted: bei ausgeschaltetem Kopfbereich → wird kein Bild geladen.</summary>
    [Fact]
    public void LoginCompleted_WithHeroDisabled_SkipsHeroLoad()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = "http://nas/videoplayer/",
            Settings = new AppSettings { HomeHeroEnabled = false }
        };

        var transition = AppUpdate.Update(model, new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.DoesNotContain(transition.Effects, e => e is LoadHeroBackgroundEffect);
        Assert.False(transition.Model.HomeHero.IsLoading);
        Assert.False(transition.Model.ShowHomeHero);
    }

    /// <summary>Prüft LoginCompleted: bei bereits geladenem Bild → wird nicht erneut geladen.</summary>
    [Fact]
    public void LoginCompleted_WithExistingImage_SkipsHeroLoad()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = "http://nas/videoplayer/",
            HomeHero = new HomeHeroState { ImageBytes = [1, 2, 3] }
        };

        var transition = AppUpdate.Update(model, new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.DoesNotContain(transition.Effects, e => e is LoadHeroBackgroundEffect);
        Assert.True(transition.Model.HomeHero.HasImage);
    }

    /// <summary>Prüft HeroBackgroundLoaded: übernimmt die Bilddaten ins Model.</summary>
    [Fact]
    public void HeroBackgroundLoaded_WithBytes_SetsImageAndStopsLoading()
    {
        var model = ConnectedModel() with
        {
            HomeHero = new HomeHeroState { IsLoading = true }
        };

        var transition = AppUpdate.Update(
            model,
            new HeroBackgroundLoadedMessage([9, 8, 7]));

        Assert.False(transition.Model.HomeHero.IsLoading);
        Assert.Equal([9, 8, 7], transition.Model.HomeHero.ImageBytes);
        Assert.True(transition.Model.HomeHero.HasImage);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft HeroBackgroundLoaded: bei Fehlschlag (null) → bleibt der Zustand stabil ohne Bild.</summary>
    [Fact]
    public void HeroBackgroundLoaded_WithoutImage_StaysStableWithoutImage()
    {
        var model = ConnectedModel() with
        {
            HomeHero = new HomeHeroState { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new HeroBackgroundLoadedMessage(null));

        // Kein Absturz/Layout-Bruch: der Kopfbereich bleibt sichtbar,
        // zeigt aber nur Gradienten und Texte.
        Assert.False(transition.Model.HomeHero.IsLoading);
        Assert.Null(transition.Model.HomeHero.ImageBytes);
        Assert.False(transition.Model.HomeHero.HasImage);
        Assert.True(transition.Model.ShowHomeHero);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft HeroBackgroundLoaded: ein Fehlschlag löscht kein bereits vorhandenes Bild.</summary>
    [Fact]
    public void HeroBackgroundLoaded_Failure_KeepsExistingImage()
    {
        var model = ConnectedModel() with
        {
            HomeHero = new HomeHeroState { IsLoading = true, ImageBytes = [4, 5] }
        };

        var transition = AppUpdate.Update(model, new HeroBackgroundLoadedMessage(null));

        Assert.Equal([4, 5], transition.Model.HomeHero.ImageBytes);
        Assert.True(transition.Model.HomeHero.HasImage);
    }

    /// <summary>Prüft HeroBackgroundLoaded: eine verspätete Antwort ohne laufenden Ladevorgang wird ignoriert.</summary>
    [Fact]
    public void HeroBackgroundLoaded_WithoutActiveLoad_IsIgnored()
    {
        // Nach Abmeldung/Sitzungsablauf/Serverwechsel wurde der
        // Hero-Zustand zurückgesetzt — eine nachträglich eintreffende
        // Antwort darf kein sitzungsfremdes Bild hinterlassen.
        var model = ConnectedModel();

        var transition = AppUpdate.Update(model, new HeroBackgroundLoadedMessage([1, 2]));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft LogoutRequested: setzt den Hero-Zustand der beendeten Sitzung zurück.</summary>
    [Fact]
    public void LogoutRequested_ResetsHomeHero()
    {
        var model = ConnectedModel() with
        {
            HomeHero = new HomeHeroState { ImageBytes = [9, 9] }
        };

        var transition = AppUpdate.Update(model, new LogoutRequestedMessage());

        Assert.Equal(HomeHeroState.Empty, transition.Model.HomeHero);
        Assert.False(transition.Model.HomeHero.HasImage);
    }

    /// <summary>Prüft SaveSettingsRequested: das Einschalten des Kopfbereichs im verbundenen Zustand lädt das Bild nach.</summary>
    [Fact]
    public void SaveSettings_HeroEnabledWhileConnected_LoadsHeroBackground()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { HomeHeroEnabled = false }
        };

        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(model.Settings with { HomeHeroEnabled = true }));

        Assert.True(transition.Model.Settings.HomeHeroEnabled);
        Assert.True(transition.Model.ShowHomeHero);
        Assert.Contains(transition.Effects, e => e is PersistSettingsEffect);
        Assert.Contains(transition.Effects, e => e is LoadHeroBackgroundEffect);
        Assert.True(transition.Model.HomeHero.IsLoading);
    }

    /// <summary>Prüft SaveSettingsRequested: das Ausschalten blendet den Kopfbereich aus (persistiert, kein Ladevorgang).</summary>
    [Fact]
    public void SaveSettings_HeroDisabledWhileConnected_HidesHeroWithoutLoad()
    {
        var model = ConnectedModel() with
        {
            HomeHero = new HomeHeroState { ImageBytes = [1] }
        };

        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(model.Settings with { HomeHeroEnabled = false }));

        Assert.False(transition.Model.Settings.HomeHeroEnabled);
        Assert.False(transition.Model.ShowHomeHero);
        Assert.DoesNotContain(transition.Effects, e => e is LoadHeroBackgroundEffect);
        // Das Bild bleibt im flüchtigen Zustand — beim erneuten
        // Einschalten ist es sofort wieder da (kein erneuter Abruf nötig).
        Assert.True(transition.Model.HomeHero.HasImage);
    }

    /// <summary>Prüft SaveSettingsRequested: das Einschalten ohne Verbindung löst keinen Abruf aus (folgt beim Verbindungsaufbau).</summary>
    [Fact]
    public void SaveSettings_HeroEnabledWhileOffline_DoesNotLoad()
    {
        var model = ConnectedModel() with
        {
            ConnectionState = ConnectionState.Offline,
            Settings = new AppSettings { HomeHeroEnabled = false }
        };

        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(model.Settings with { HomeHeroEnabled = true }));

        Assert.True(transition.Model.ShowHomeHero);
        Assert.DoesNotContain(transition.Effects, e => e is LoadHeroBackgroundEffect);
    }

    /// <summary>Prüft SaveSettingsRequested: das Einschalten mit bereits geladenem Bild löst keinen erneuten Abruf aus.</summary>
    [Fact]
    public void SaveSettings_HeroEnabledWithExistingImage_DoesNotReload()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { HomeHeroEnabled = false },
            HomeHero = new HomeHeroState { ImageBytes = [7, 7] }
        };

        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(model.Settings with { HomeHeroEnabled = true }));

        Assert.DoesNotContain(transition.Effects, e => e is LoadHeroBackgroundEffect);
    }

    /// <summary>Prüft HeroTitle: der Anzeigename des aktiven Servers trägt den Hero-Titel.</summary>
    [Fact]
    public void HeroTitle_UsesActiveServerDisplayName()
    {
        var model = ConnectedModel() with
        {
            KnownServers =
            [
                new KnownServer
                {
                    Address = "http://nas/videoplayer/",
                    DisplayName = "Martins Videosammlung",
                    LastUsedAtUtc = System.DateTime.UtcNow
                }
            ]
        };

        Assert.Equal("Martins Videosammlung", model.HeroTitle);
    }

    /// <summary>Prüft HeroTitle: ohne Anzeigenamen fällt der Titel auf die Serveradresse zurück.</summary>
    [Fact]
    public void HeroTitle_WithoutDisplayName_FallsBackToAddress()
    {
        var model = ConnectedModel() with
        {
            KnownServers =
            [
                new KnownServer
                {
                    Address = "http://nas/videoplayer/",
                    LastUsedAtUtc = System.DateTime.UtcNow
                }
            ]
        };

        Assert.Equal("http://nas/videoplayer/", model.HeroTitle);
    }

    /// <summary>Prüft HeroTitle: ohne aktiven Server → App-Name als Fallback.</summary>
    [Fact]
    public void HeroTitle_WithoutActiveServer_FallsBackToAppName()
    {
        Assert.Equal("VideoPlayer", new AppModel().HeroTitle);
    }
}
