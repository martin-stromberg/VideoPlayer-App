using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Download-Verwaltung (Schritt 11): Downloads-Seite,
/// Einreihen/Abbrechen/Wiederholen/Löschen, automatische Wiederaufnahme,
/// Trigger der automatischen Downloads und das automatische Entfernen
/// gesehener Episoden.
/// </summary>
public class AppUpdateTests_Downloads
{
    private const string Server = "http://server/videoplayer/";

    private static AppModel ConnectedModel(AppPage page = AppPage.Home) => new()
    {
        ConnectionState = ConnectionState.Connected,
        ActiveServerAddress = Server,
        CurrentPage = page,
        IsOnline = true,
        IsStartupRunning = false
    };

    private static DownloadListItem Item(
        long mediaId = 42,
        string mediaType = "movie",
        DownloadState state = DownloadState.Downloading) => new()
        {
            EntryId = (int)mediaId,
            MediaType = mediaType,
            MediaId = mediaId,
            Title = $"Titel {mediaId}",
            State = state
        };

    /// <summary>Prüft PageOpened: lädt die Liste aus dem Manifest.</summary>
    [Fact]
    public void DownloadsPageOpened_LoadsManifestList()
    {
        var transition = AppUpdate.Update(ConnectedModel(), new DownloadsPageOpenedMessage());

        Assert.True(transition.Model.Downloads.IsLoading);
        Assert.True(transition.Model.Downloads.Attempted);
        Assert.Contains(transition.Effects, e => e is LoadDownloadsEffect);
    }

    /// <summary>Prüft PageOpened: bei laufendem Lauf kein zweiter.</summary>
    [Fact]
    public void DownloadsPageOpened_WhileLoading_StaysUnchanged()
    {
        var model = ConnectedModel() with
        {
            Downloads = DownloadsPageState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new DownloadsPageOpenedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Loaded: übernimmt Einträge, Belegung und Kontingent.</summary>
    [Fact]
    public void DownloadsLoaded_AppliesItemsAndQuota()
    {
        var items = new List<DownloadListItem> { Item(1), Item(2) };
        var transition = AppUpdate.Update(
            ConnectedModel() with { Downloads = DownloadsPageState.Initial with { IsLoading = true, Attempted = true } },
            new DownloadsLoadedMessage(items, 2048, 7, null));

        Assert.Equal(2, transition.Model.Downloads.Items.Count);
        Assert.Equal(2048, transition.Model.Downloads.UsedBytes);
        Assert.Equal(7, transition.Model.Downloads.QuotaGb);
        Assert.False(transition.Model.Downloads.IsLoading);
        Assert.True(transition.Model.Downloads.Attempted);
    }

    /// <summary>Prüft Loaded: Fehler werden in den Zustand übernommen.</summary>
    [Fact]
    public void DownloadsLoaded_Error_SetsErrorState()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadsLoadedMessage([], 0, 5, "Lesefehler"));

        Assert.True(transition.Model.Downloads.HasError);
        Assert.True(transition.Model.Downloads.ShowEmpty is false);
    }

    /// <summary>Prüft Requested: verbunden → Einreihe-Effekt.</summary>
    [Fact]
    public void DownloadRequested_Connected_EnqueuesDownload()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRequestedMessage("Movie", 42, "Film"));

        var effect = Assert.Single(transition.Effects);
        var enqueue = Assert.IsType<EnqueueDownloadEffect>(effect);
        Assert.Equal("movie", enqueue.MediaType);
        Assert.Equal(42, enqueue.MediaId);
        Assert.Equal("Film", enqueue.Title);
    }

    /// <summary>Prüft Requested: Episoden sind ladbar.</summary>
    [Fact]
    public void DownloadRequested_Episode_EnqueuesDownload()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRequestedMessage("episode", 7, "Folge"));

        var enqueue = Assert.IsType<EnqueueDownloadEffect>(Assert.Single(transition.Effects));
        Assert.Equal("tvshowepisode", enqueue.MediaType);
        Assert.Equal(7, enqueue.MediaId);
    }

    /// <summary>Prüft Requested: nicht abspielbare Typen werden abgewiesen.</summary>
    [Fact]
    public void DownloadRequested_NonPlayableType_Unchanged()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRequestedMessage("tvshow", 5, "Serie"));

        Assert.Same(ConnectedModel().GetType(), transition.Model.GetType());
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Requested: ungültige Id wird abgewiesen.</summary>
    [Fact]
    public void DownloadRequested_InvalidId_Unchanged()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRequestedMessage("movie", 0, "Film"));

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Requested: offline → Hinweis statt Effekt.</summary>
    [Fact]
    public void DownloadRequested_Offline_ShowsNoticeWithoutEffect()
    {
        var model = ConnectedModel() with { ConnectionState = ConnectionState.Offline };
        var transition = AppUpdate.Update(
            model,
            new DownloadRequestedMessage("movie", 42, "Film"));

        Assert.Empty(transition.Effects);
        Assert.True(transition.Model.MediaDetails.DownloadNoticeIsError);
        Assert.NotNull(transition.Model.MediaDetails.DownloadNotice);
    }

    /// <summary>Prüft EnqueueCompleted: Eintrag landet in der Liste.</summary>
    [Fact]
    public void DownloadEnqueueCompleted_UpsertsItem()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadEnqueueCompletedMessage(
                Item(42, state: DownloadState.Queued), "eingeplant", false, "movie", 42));

        var item = Assert.Single(transition.Model.Downloads.Items);
        Assert.Equal(42, item.MediaId);
        Assert.Equal(DownloadState.Queued, item.State);
    }

    /// <summary>Prüft EnqueueCompleted: Hinweis bei passender Detailseite.</summary>
    [Fact]
    public void DownloadEnqueueCompleted_MatchingDetails_SetsNotice()
    {
        var model = ConnectedModel() with
        {
            MediaDetails = new MediaDetailsState { MediaType = "movie", MediaId = 42 }
        };
        var transition = AppUpdate.Update(
            model,
            new DownloadEnqueueCompletedMessage(
                Item(42, state: DownloadState.Queued), "Der Download wurde eingeplant.", false, "movie", 42));

        Assert.Equal("Der Download wurde eingeplant.", transition.Model.MediaDetails.DownloadNotice);
        Assert.False(transition.Model.MediaDetails.DownloadNoticeIsError);
    }

    /// <summary>Prüft EnqueueCompleted: Hinweis fremder Identität bleibt unberührt.</summary>
    [Fact]
    public void DownloadEnqueueCompleted_OtherDetails_IgnoresNotice()
    {
        var model = ConnectedModel() with
        {
            MediaDetails = new MediaDetailsState { MediaType = "movie", MediaId = 7 }
        };
        var transition = AppUpdate.Update(
            model,
            new DownloadEnqueueCompletedMessage(
                Item(42, state: DownloadState.Queued), "Der Download wurde eingeplant.", false, "movie", 42));

        Assert.Null(transition.Model.MediaDetails.DownloadNotice);
    }

    /// <summary>Prüft ItemChanged: ersetzt den Eintrag in der Liste.</summary>
    [Fact]
    public void DownloadItemChanged_UpsertsProgress()
    {
        var model = ConnectedModel() with
        {
            Downloads = DownloadsPageState.Initial with { Items = [Item(42)] }
        };
        var updated = Item(42) with { DownloadedBytes = 50, TotalBytes = 100 };
        var transition = AppUpdate.Update(model, new DownloadItemChangedMessage(updated));

        var item = Assert.Single(transition.Model.Downloads.Items);
        Assert.Equal(0.5, item.Progress, 3);
    }

    /// <summary>Prüft Deleted: entfernt den Eintrag aus der Liste.</summary>
    [Fact]
    public void DownloadDeleted_RemovesItem()
    {
        var model = ConnectedModel() with
        {
            Downloads = DownloadsPageState.Initial with { Items = [Item(42), Item(7)] }
        };
        var transition = AppUpdate.Update(model, new DownloadDeletedMessage("movie", 42));

        var item = Assert.Single(transition.Model.Downloads.Items);
        Assert.Equal(7, item.MediaId);
    }

    /// <summary>Prüft Cancel: laufender Download → Abbruch-Effekt.</summary>
    [Fact]
    public void DownloadCancelRequested_Active_EmitsCancelEffect()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadCancelRequestedMessage(Item(42)));

        var effect = Assert.IsType<CancelDownloadEffect>(Assert.Single(transition.Effects));
        Assert.Equal(42, effect.MediaId);
    }

    /// <summary>Prüft Cancel: abgeschlossener Download → kein Effekt.</summary>
    [Fact]
    public void DownloadCancelRequested_Completed_Unchanged()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadCancelRequestedMessage(Item(42, state: DownloadState.Completed)));

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Retry: fehlgeschlagener Download → erneutes Einreihen.</summary>
    [Fact]
    public void DownloadRetryRequested_Failed_EnqueuesAgain()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRetryRequestedMessage(Item(42, state: DownloadState.Failed)));

        var effect = Assert.IsType<EnqueueDownloadEffect>(Assert.Single(transition.Effects));
        Assert.Equal(42, effect.MediaId);
    }

    /// <summary>Prüft Retry: laufender Download → kein Effekt.</summary>
    [Fact]
    public void DownloadRetryRequested_Active_Unchanged()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRetryRequestedMessage(Item(42)));

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Delete: Lösch-Effekt für den Eintrag.</summary>
    [Fact]
    public void DownloadDeleteRequested_EmitsDeleteEffect()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadDeleteRequestedMessage(Item(42)));

        var effect = Assert.IsType<DeleteDownloadEffect>(Assert.Single(transition.Effects));
        Assert.Equal("movie", effect.MediaType);
        Assert.Equal(42, effect.MediaId);
    }

    /// <summary>Prüft LoginCompleted: erfolgreiche Anmeldung → Wiederaufnahme-Effekt.</summary>
    [Fact]
    public void LoginCompleted_Success_ResumesDownloads()
    {
        var transition = AppUpdate.Update(
            new AppModel { ActiveServerAddress = Server },
            new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.Contains(transition.Effects, e => e is ResumeDownloadsEffect);
    }

    /// <summary>Prüft NetworkOnline: im verbundenen Zustand → Wiederaufnahme (WLAN-Rückkehr).</summary>
    [Fact]
    public void NetworkOnline_WhileConnected_ResumesDownloads()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new NetworkAvailabilityChangedMessage(true));

        Assert.Contains(transition.Effects, e => e is ResumeDownloadsEffect);
    }

    /// <summary>Prüft SyncCompleted: aktivierte Auto-Downloads → Vorlauf-Effekt.</summary>
    [Fact]
    public void SyncCompleted_AutoDownloadsEnabled_RunsAutoDownloads()
    {
        var model = ConnectedModel() with
        {
            IsSyncRunning = true,
            Settings = new AppSettings { AutoDownloadEnabled = true }
        };
        var transition = AppUpdate.Update(
            model,
            new SynchronizationCompletedMessage(new SyncResult(0, 0, true, true, true)));

        Assert.False(transition.Model.IsSyncRunning);
        Assert.Contains(transition.Effects, e => e is LoadHomeListsEffect);
        Assert.Contains(transition.Effects, e => e is RunAutoDownloadsEffect);
    }

    /// <summary>Prüft SyncCompleted: deaktivierte Auto-Downloads → kein Vorlauf.</summary>
    [Fact]
    public void SyncCompleted_AutoDownloadsDisabled_NoAutoRun()
    {
        var model = ConnectedModel() with
        {
            IsSyncRunning = true,
            Settings = new AppSettings { AutoDownloadEnabled = false }
        };
        var transition = AppUpdate.Update(
            model,
            new SynchronizationCompletedMessage(new SyncResult(0, 0, true, true, true)));

        Assert.DoesNotContain(transition.Effects, e => e is RunAutoDownloadsEffect);
    }

    /// <summary>Prüft CachedListsChanged: aktivierte Auto-Downloads → Vorlauf-Effekt.</summary>
    [Fact]
    public void CachedListsChanged_AutoDownloadsEnabled_RunsAutoDownloads()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDownloadEnabled = true }
        };
        var transition = AppUpdate.Update(model, new CachedListsChangedMessage());

        Assert.Contains(transition.Effects, e => e is LoadHomeListsEffect);
        Assert.Contains(transition.Effects, e => e is RunAutoDownloadsEffect);
    }

    /// <summary>Prüft CachedListsChanged: ohne Verbindung → kein Vorlauf.</summary>
    [Fact]
    public void CachedListsChanged_NotConnected_NoAutoRun()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            Settings = new AppSettings { AutoDownloadEnabled = true }
        };
        var transition = AppUpdate.Update(model, new CachedListsChangedMessage());

        Assert.DoesNotContain(transition.Effects, e => e is RunAutoDownloadsEffect);
    }

    /// <summary>Prüft SaveSettings: Einschalten der Auto-Downloads → erzwungener Vorlauf.</summary>
    [Fact]
    public void SaveSettings_EnablingAutoDownloads_ForcesAutoRun()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDownloadEnabled = false }
        };
        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(new AppSettings { AutoDownloadEnabled = true }));

        var effect = Assert.Single(transition.Effects, e => e is RunAutoDownloadsEffect);
        Assert.True(((RunAutoDownloadsEffect)effect).Force);
    }

    /// <summary>Prüft SaveSettings: bereits aktive Auto-Downloads → kein zusätzlicher Lauf.</summary>
    [Fact]
    public void SaveSettings_AlreadyEnabled_NoForcedRun()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDownloadEnabled = true }
        };
        var transition = AppUpdate.Update(
            model,
            new SaveSettingsRequestedMessage(new AppSettings { AutoDownloadEnabled = true }));

        Assert.DoesNotContain(transition.Effects, e => e is RunAutoDownloadsEffect);
    }

    /// <summary>Prüft Progress: gesehene Episode + Schalter → Lösch-Effekt.</summary>
    [Fact]
    public void PlaybackProgress_WatchedEpisode_EmitsAutoDelete()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDeleteWatchedEpisodes = true },
            Player = new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 33,
                Attempted = true
            }
        };
        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                TimeSpan.FromMinutes(88), TimeSpan.FromMinutes(90), true, false));

        Assert.True(transition.Model.Player.IsWatched);
        Assert.Contains(transition.Effects, e =>
            e is DeleteWatchedDownloadEffect d && d.MediaId == 33);
    }

    /// <summary>Prüft Progress: gesehener Film → kein Auto-Lösch-Effekt.</summary>
    [Fact]
    public void PlaybackProgress_WatchedMovie_NoAutoDelete()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDeleteWatchedEpisodes = true },
            Player = new PlayerState
            {
                MediaType = "movie",
                MediaId = 33,
                Attempted = true
            }
        };
        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                TimeSpan.FromMinutes(88), TimeSpan.FromMinutes(90), true, false));

        Assert.DoesNotContain(transition.Effects, e => e is DeleteWatchedDownloadEffect);
    }

    /// <summary>Prüft Progress: deaktivierter Schalter → kein Auto-Lösch-Effekt.</summary>
    [Fact]
    public void PlaybackProgress_AutoDeleteDisabled_NoEffect()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDeleteWatchedEpisodes = false },
            Player = new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 33,
                Attempted = true
            }
        };
        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                TimeSpan.FromMinutes(88), TimeSpan.FromMinutes(90), true, false));

        Assert.DoesNotContain(transition.Effects, e => e is DeleteWatchedDownloadEffect);
    }

    /// <summary>Prüft Ended: gesehene Episode + Schalter → Lösch-Effekt.</summary>
    [Fact]
    public void PlaybackEnded_WatchedEpisode_EmitsAutoDelete()
    {
        var model = ConnectedModel() with
        {
            Settings = new AppSettings { AutoDeleteWatchedEpisodes = true },
            Player = new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 33,
                Attempted = true,
                Duration = TimeSpan.FromMinutes(40),
                // Position am Medienende — das Ende-Ereignis gilt nur
                // innerhalb des Toleranzfensters als Wiedergabeende.
                Position = TimeSpan.FromMinutes(40)
            }
        };
        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.True(transition.Model.Player.IsWatched);
        Assert.Contains(transition.Effects, e =>
            e is DeleteWatchedDownloadEffect d && d.MediaId == 33);
    }
}
