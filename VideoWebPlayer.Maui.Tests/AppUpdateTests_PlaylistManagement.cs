using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Playlist-Verwaltung (Issue 43, Schritt 6):
/// Formular (Anlage/Bearbeitung mit Namens-Pflicht), Löschen mit
/// Bestätigung, Eintrags-Entfernen mit Weiterschauen-Konflikt (HTTP 409),
/// Verschieben im manuellen Sortiermodus, Sortiermodus-Wechsel mit
/// Verlust-Konflikt, Genre-Editor, Öffentlich-Flag mit Admin-Wache
/// sowie „Zu Playlist hinzufügen" der Medien-Detailseite.
/// </summary>
public class AppUpdateTests_PlaylistManagement
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static PlaylistListItem Item(long id, string name = "Liste") =>
        new() { PlaylistId = id, Name = name };

    private static PlaylistEntryItem Entry(long id, long? sortOrder = null) =>
        new()
        {
            EntryId = id,
            MediaId = 100 + id,
            MediaType = "movie",
            Title = $"Eintrag {id}",
            SortOrder = sortOrder
        };

    /// <summary>Detail-Zustand einer eigenen Playlist im manuellen Modus mit zwei Einträgen.</summary>
    /// <param name="entries">Die geladenen Einträge der Playlist.</param>
    /// <returns>Der befüllte Detail-Zustand.</returns>
    private static PlaylistDetailsState OwnManualDetails(params PlaylistEntryItem[] entries) =>
        PlaylistDetailsState.Empty with
        {
            PlaylistId = 7,
            Name = "Liste",
            SortMode = VideoPlayer.ApiClient.Models.PlaylistSortModeValues.Manual,
            IsOwner = true,
            Entries = entries,
            Attempted = true
        };

    // --- Playlist-Formular ---

    /// <summary>Prüft die Anlage-Anforderung: öffnet das leere Formular mit wählbarem Sortiermodus.</summary>
    [Fact]
    public void PlaylistCreateRequested_OpensEmptyForm()
    {
        var (next, effects) = AppUpdate.Update(Connected(), new PlaylistCreateRequestedMessage());

        Assert.True(next.PlaylistForm.IsOpen);
        Assert.False(next.PlaylistForm.IsEditing);
        Assert.Null(next.PlaylistForm.EditingPlaylistId);
        Assert.Equal(VideoPlayer.ApiClient.Models.PlaylistSortModeValues.Manual, next.PlaylistForm.SortMode);
        Assert.Empty(effects);
    }

    /// <summary>Prüft die Bearbeitungs-Anforderung: öffnet das Formular mit den Kopfdaten der eigenen Playlist.</summary>
    [Fact]
    public void PlaylistEditRequested_OwnPlaylist_OpensPrefilledForm()
    {
        var details = OwnManualDetails() with { Description = "Alt" };
        var model = Connected() with { PlaylistDetails = details };

        var (next, _) = AppUpdate.Update(model, new PlaylistEditRequestedMessage());

        Assert.True(next.PlaylistForm.IsOpen);
        Assert.True(next.PlaylistForm.IsEditing);
        Assert.Equal(7, next.PlaylistForm.EditingPlaylistId);
        Assert.Equal("Liste", next.PlaylistForm.Name);
        Assert.Equal("Alt", next.PlaylistForm.Description);
    }

    /// <summary>Prüft die Bearbeitungs-Wache: fremde Playlists öffnen kein Formular.</summary>
    [Fact]
    public void PlaylistEditRequested_ForeignPlaylist_StaysUnchanged()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { IsOwner = false }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistEditRequestedMessage());

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft die Namens-Pflicht: ein leerer Name bleibt mit Inline-Fehltext ohne Effekt im Formular.</summary>
    [Fact]
    public void PlaylistFormSaveRequested_EmptyName_ShowsInlineError()
    {
        var model = Connected() with
        {
            PlaylistForm = new PlaylistFormState { IsOpen = true, Name = "  " }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistFormSaveRequestedMessage());

        Assert.Equal(AppUpdate.PlaylistNameRequiredError, next.PlaylistForm.Error);
        Assert.False(next.PlaylistForm.IsSaving);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Speichern der Anlage: trägt die Felder in den Save-Effekt.</summary>
    [Fact]
    public void PlaylistFormSaveRequested_Create_EmitsSaveEffect()
    {
        var model = Connected() with
        {
            PlaylistForm = new PlaylistFormState
            {
                IsOpen = true,
                Name = " Meine Liste ",
                Description = "Beschreibung"
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistFormSaveRequestedMessage());

        Assert.True(next.PlaylistForm.IsSaving);
        var effect = Assert.IsType<SavePlaylistEffect>(Assert.Single(effects));
        Assert.Null(effect.EditingPlaylistId);
        Assert.Equal("Meine Liste", effect.Name);
        Assert.Equal("Beschreibung", effect.Description);
    }

    /// <summary>Prüft das Speichern der Bearbeitung: trägt die Playlist-Id in den Save-Effekt.</summary>
    [Fact]
    public void PlaylistFormSaveRequested_Edit_EmitsUpdateEffect()
    {
        var model = Connected() with
        {
            PlaylistForm = new PlaylistFormState
            {
                IsOpen = true,
                EditingPlaylistId = 7,
                Name = "Neu"
            }
        };

        var (_, effects) = AppUpdate.Update(model, new PlaylistFormSaveRequestedMessage());

        var effect = Assert.IsType<SavePlaylistEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.EditingPlaylistId);
    }

    /// <summary>Prüft das Formular-Ergebnis: Fehlschlag lässt das Formular mit Fehltext offen.</summary>
    [Fact]
    public void PlaylistFormCompleted_Error_KeepsFormOpen()
    {
        var model = Connected() with
        {
            PlaylistForm = new PlaylistFormState { IsOpen = true, IsSaving = true }
        };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistFormCompletedMessage(null, "Fehler"));

        Assert.True(next.PlaylistForm.IsOpen);
        Assert.False(next.PlaylistForm.IsSaving);
        Assert.Equal("Fehler", next.PlaylistForm.Error);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Formular-Ergebnis: Erfolg schließt das Formular und lädt die Übersicht neu.</summary>
    [Fact]
    public void PlaylistFormCompleted_Success_ClosesAndReloadsOverview()
    {
        var model = Connected() with
        {
            PlaylistForm = new PlaylistFormState { IsOpen = true, IsSaving = true }
        };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistFormCompletedMessage(Item(9), null));

        Assert.False(next.PlaylistForm.IsOpen);
        Assert.IsType<LoadPlaylistsEffect>(Assert.Single(effects));
    }

    // --- Löschen ---

    /// <summary>Prüft die Lösch-Bestätigung: eigene Playlist startet den Lösch-Effekt.</summary>
    [Fact]
    public void PlaylistDeleteConfirmed_OwnPlaylist_EmitsDelete()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDeleteConfirmedMessage(7));

        Assert.True(next.PlaylistDetails.IsManaging);
        var effect = Assert.IsType<DeletePlaylistEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
    }

    /// <summary>Prüft die Lösch-Wache: fremde Playlists löschen nichts.</summary>
    [Fact]
    public void PlaylistDeleteConfirmed_ForeignPlaylist_StaysUnchanged()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { IsOwner = false }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDeleteConfirmedMessage(7));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Lösch-Ergebnis: Erfolg leert den Detail-Zustand, navigiert zurück und lädt die Übersicht neu.</summary>
    [Fact]
    public void PlaylistDeleted_Success_ClearsState_NavigatesBack_Reloads()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDeletedMessage(7, null));

        Assert.False(next.PlaylistDetails.IsActive);
        Assert.Equal(2, effects.Count);
        Assert.IsType<NavigateBackEffect>(effects[0]);
        Assert.IsType<LoadPlaylistsEffect>(effects[1]);
    }

    /// <summary>Prüft das Lösch-Ergebnis: Fehlschlag zeigt den Fehlerhinweis.</summary>
    [Fact]
    public void PlaylistDeleted_Error_ShowsNotice()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDeletedMessage(7, "Fehler"));

        Assert.True(next.PlaylistDetails.IsActive);
        Assert.False(next.PlaylistDetails.IsManaging);
        Assert.Equal("Fehler", next.PlaylistDetails.ManageNotice);
        Assert.True(next.PlaylistDetails.ManageNoticeIsError);
        Assert.Empty(effects);
    }

    // --- Eintrag entfernen (inkl. 409-Konflikt) ---

    /// <summary>Prüft die Entfernen-Anforderung: eigener Eintrag startet den Effekt ohne Flagge.</summary>
    [Fact]
    public void PlaylistEntryRemoveRequested_EmitsRemoveWithoutConfirm()
    {
        var entry = Entry(1, sortOrder: 0);
        var model = Connected() with { PlaylistDetails = OwnManualDetails(entry, Entry(2, 1)) };

        var (next, effects) = AppUpdate.Update(model, new PlaylistEntryRemoveRequestedMessage(entry));

        Assert.True(next.PlaylistDetails.IsManaging);
        var effect = Assert.IsType<RemovePlaylistEntryEffect>(Assert.Single(effects));
        Assert.False(effect.ConfirmContinueWatchingRemoval);
        Assert.Same(entry, effect.Entry);
    }

    /// <summary>Prüft das Entfernen-Ergebnis: der 409-Konflikt parkt den Eintrag als ausstehende Bestätigung.</summary>
    [Fact]
    public void PlaylistEntryRemoveCompleted_Conflict_SetsPending()
    {
        var entry = Entry(1, sortOrder: 0);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(entry) with { IsManaging = true }
        };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistEntryRemoveCompletedMessage(entry, Removed: false, RequiresConfirmation: true, Error: null));

        Assert.False(next.PlaylistDetails.IsManaging);
        Assert.Same(entry, next.PlaylistDetails.PendingRemoveConflict);
        Assert.Empty(effects);
    }

    /// <summary>Prüft die Bestätigungs-Entscheidung: bestätigt wiederholt das Entfernen mit der Flagge.</summary>
    [Fact]
    public void PlaylistEntryRemoveConfirmed_True_RetriesWithConfirm()
    {
        var entry = Entry(1, sortOrder: 0);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(entry) with { PendingRemoveConflict = entry }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistEntryRemoveConfirmedMessage(true));

        Assert.Null(next.PlaylistDetails.PendingRemoveConflict);
        Assert.True(next.PlaylistDetails.IsManaging);
        var effect = Assert.IsType<RemovePlaylistEntryEffect>(Assert.Single(effects));
        Assert.True(effect.ConfirmContinueWatchingRemoval);
    }

    /// <summary>Prüft die Bestätigungs-Entscheidung: Abbruch löst den Konflikt ohne Serveraufruf.</summary>
    [Fact]
    public void PlaylistEntryRemoveConfirmed_False_ClearsPending()
    {
        var entry = Entry(1, sortOrder: 0);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(entry) with { PendingRemoveConflict = entry }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistEntryRemoveConfirmedMessage(false));

        Assert.Null(next.PlaylistDetails.PendingRemoveConflict);
        Assert.False(next.PlaylistDetails.IsManaging);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Entfernen-Ergebnis: Erfolg lädt die Eintragsliste neu.</summary>
    [Fact]
    public void PlaylistEntryRemoveCompleted_Removed_ReloadsEntries()
    {
        var entry = Entry(1, sortOrder: 0);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(entry) with { IsManaging = true }
        };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistEntryRemoveCompletedMessage(entry, Removed: true, RequiresConfirmation: false, Error: null));

        Assert.True(next.PlaylistDetails.IsLoading);
        var effect = Assert.IsType<LoadPlaylistEntriesEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
    }

    // --- Verschieben ---

    /// <summary>Prüft „An den Anfang": im manuellen Modus startet der Effekt — der erste Eintrag bleibt ausgenommen.</summary>
    [Fact]
    public void MoveToBeginning_ManualMode_EmitsMove()
    {
        var first = Entry(1, 0);
        var second = Entry(2, 1);
        var model = Connected() with { PlaylistDetails = OwnManualDetails(first, second) };

        var (_, effects) = AppUpdate.Update(model, new PlaylistEntryMoveToBeginningRequestedMessage(second));
        Assert.IsType<MovePlaylistEntryToBeginningEffect>(Assert.Single(effects));

        // Der erste Eintrag hat kein „An den Anfang".
        var (same, none) = AppUpdate.Update(model, new PlaylistEntryMoveToBeginningRequestedMessage(first));
        Assert.Same(model, same);
        Assert.Empty(none);
    }

    /// <summary>Prüft die Verschiebe-Wache: im Erscheinungsdatum-Modus gibt es keine Verschiebe-Aktionen.</summary>
    [Fact]
    public void MoveToBeginning_ByReleaseDate_StaysUnchanged()
    {
        var details = OwnManualDetails(Entry(1, 0), Entry(2, 1)) with
        {
            SortMode = VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate
        };
        var model = Connected() with { PlaylistDetails = details };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistEntryMoveToBeginningRequestedMessage(details.Entries[1]));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft „Eins nach vorn/hinten": trägt die Nachbar-SortOrder in den Between-Effekt.</summary>
    [Fact]
    public void MoveBetween_EmitsNeighborSortOrder()
    {
        var entry = Entry(2, 1);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(Entry(1, 0), entry, Entry(3, 2))
        };

        var (_, effects) = AppUpdate.Update(
            model, new PlaylistEntryMoveBetweenRequestedMessage(entry, 0));
        var effect = Assert.IsType<MovePlaylistEntryBetweenEffect>(Assert.Single(effects));
        Assert.Equal(0, effect.NewSortOrder);
    }

    /// <summary>Prüft das Verschiebe-Ergebnis: Erfolg lädt die Eintragsliste neu.</summary>
    [Fact]
    public void PlaylistEntryMoveCompleted_Success_ReloadsEntries()
    {
        var entry = Entry(1, 0);
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(entry) with { IsManaging = true }
        };

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistEntryMoveCompletedMessage(entry, null));

        Assert.True(next.PlaylistDetails.IsLoading);
        Assert.IsType<LoadPlaylistEntriesEffect>(Assert.Single(effects));
    }

    // --- Sortiermodus ---

    /// <summary>Prüft den Sortiermodus-Wechsel: anderer Modus startet den Effekt ohne Flagge.</summary>
    [Fact]
    public void PlaylistSortModeRequested_EmitsChange()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model,
            new PlaylistSortModeRequestedMessage(VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate));

        Assert.True(next.PlaylistDetails.IsManaging);
        var effect = Assert.IsType<ChangePlaylistSortModeEffect>(Assert.Single(effects));
        Assert.False(effect.ConfirmLossOfManualOrder);
    }

    /// <summary>Prüft den Sortiermodus-Wechsel: identischer Modus ändert nichts.</summary>
    [Fact]
    public void PlaylistSortModeRequested_SameMode_StaysUnchanged()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model,
            new PlaylistSortModeRequestedMessage(VideoPlayer.ApiClient.Models.PlaylistSortModeValues.Manual));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Sortiermodus-Ergebnis: der 409-Konflikt parkt den Ziel-Modus als ausstehende Bestätigung.</summary>
    [Fact]
    public void PlaylistSortModeCompleted_Conflict_SetsPending()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { IsManaging = true }
        };

        var (next, effects) = AppUpdate.Update(model,
            new PlaylistSortModeCompletedMessage(
                VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate,
                null, RequiresConfirmation: true, Error: null));

        Assert.Equal(VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate,
            next.PlaylistDetails.PendingSortModeConflict);
        Assert.Empty(effects);
    }

    /// <summary>Prüft die Bestätigungs-Entscheidung: bestätigt wiederholt den Wechsel mit der Verlust-Flagge.</summary>
    [Fact]
    public void PlaylistSortModeConfirmed_True_RetriesConfirmed()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with
            {
                PendingSortModeConflict = VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistSortModeConfirmedMessage(true));

        Assert.Null(next.PlaylistDetails.PendingSortModeConflict);
        var effect = Assert.IsType<ChangePlaylistSortModeEffect>(Assert.Single(effects));
        Assert.True(effect.ConfirmLossOfManualOrder);
    }

    /// <summary>Prüft das Sortiermodus-Ergebnis: Erfolg übernimmt den Modus und lädt die Einträge neu.</summary>
    [Fact]
    public void PlaylistSortModeCompleted_Success_UpdatesMode_Reloads()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { IsManaging = true }
        };
        var updated = Item(7) with { SortMode = VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate };

        var (next, effects) = AppUpdate.Update(model,
            new PlaylistSortModeCompletedMessage(
                VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate,
                updated, RequiresConfirmation: false, Error: null));

        Assert.Equal(VideoPlayer.ApiClient.Models.PlaylistSortModeValues.ByReleaseDate,
            next.PlaylistDetails.SortMode);
        Assert.IsType<LoadPlaylistEntriesEffect>(Assert.Single(effects));
    }

    // --- Genre-Editor ---

    /// <summary>Prüft die Editor-Anforderung: öffnet den Editor und lädt die Optionen.</summary>
    [Fact]
    public void PlaylistGenreEditorRequested_OpensEditor_LoadsOptions()
    {
        var model = Connected() with { PlaylistDetails = OwnManualDetails() };

        var (next, effects) = AppUpdate.Update(model, new PlaylistGenreEditorRequestedMessage());

        Assert.True(next.PlaylistGenreEditor.IsOpen);
        Assert.True(next.PlaylistGenreEditor.IsLoading);
        Assert.Equal(7, next.PlaylistGenreEditor.PlaylistId);
        Assert.IsType<LoadGenreOptionsEffect>(Assert.Single(effects));
    }

    /// <summary>Prüft den Optionslauf: die Vorauswahl folgt den serverseitigen Genre-Ids der Playlist.</summary>
    [Fact]
    public void PlaylistGenreOptionsLoaded_PreselectsCurrent()
    {
        var details = OwnManualDetails() with { AllGenreIds = [2] };
        var model = Connected() with
        {
            PlaylistDetails = details,
            PlaylistGenreEditor = new PlaylistGenreEditorState
            {
                IsOpen = true,
                PlaylistId = 7,
                IsLoading = true
            }
        };

        var (next, _) = AppUpdate.Update(model, new PlaylistGenreOptionsLoadedMessage(
        [
            new PlaylistGenreOptionItem { Id = 1, Name = "Komödie" },
            new PlaylistGenreOptionItem { Id = 2, Name = "Drama" }
        ], null));

        var options = next.PlaylistGenreEditor.Options;
        Assert.False(options[0].IsSelected);
        Assert.True(options[1].IsSelected);
        Assert.False(next.PlaylistGenreEditor.IsLoading);
    }

    /// <summary>Prüft das Speichern: trägt die gewählten Ids in den Effekt.</summary>
    [Fact]
    public void PlaylistGenresSaveRequested_EmitsSelectedIds()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(),
            PlaylistGenreEditor = new PlaylistGenreEditorState
            {
                IsOpen = true,
                PlaylistId = 7,
                Options =
                [
                    new PlaylistGenreOptionItem { Id = 1, Name = "Komödie", IsSelected = true },
                    new PlaylistGenreOptionItem { Id = 2, Name = "Drama" }
                ]
            }
        };

        var (_, effects) = AppUpdate.Update(model, new PlaylistGenresSaveRequestedMessage());

        var effect = Assert.IsType<SetPlaylistGenresEffect>(Assert.Single(effects));
        Assert.Equal([1L], effect.GenreIds);
    }

    /// <summary>Prüft das Zurücksetzen: nur bei manuell gesetzten Genres wirksam.</summary>
    [Fact]
    public void PlaylistGenresResetRequested_OnlyWhenOverridden()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { GenresManuallyOverridden = true },
            PlaylistGenreEditor = new PlaylistGenreEditorState { IsOpen = true, PlaylistId = 7 }
        };

        var (_, effects) = AppUpdate.Update(model, new PlaylistGenresResetRequestedMessage());
        Assert.IsType<ResetPlaylistGenresEffect>(Assert.Single(effects));

        var withoutOverride = model with
        {
            PlaylistDetails = model.PlaylistDetails with { GenresManuallyOverridden = false }
        };
        var (same, none) = AppUpdate.Update(withoutOverride, new PlaylistGenresResetRequestedMessage());
        Assert.Same(withoutOverride, same);
        Assert.Empty(none);
    }

    /// <summary>Prüft das Genre-Ergebnis: Erfolg schließt den Editor und übernimmt die Felder der Detailseite.</summary>
    [Fact]
    public void PlaylistGenresCompleted_Success_ClosesEditor_UpdatesDetails()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails(),
            PlaylistGenreEditor = new PlaylistGenreEditorState
            {
                IsOpen = true,
                PlaylistId = 7,
                IsSaving = true
            }
        };
        var updated = Item(7) with
        {
            GenresManuallyOverridden = true,
            AllGenreIds = [3],
            GenreText = "Drama"
        };

        var (next, _) = AppUpdate.Update(model, new PlaylistGenresCompletedMessage(updated, null));

        Assert.False(next.PlaylistGenreEditor.IsOpen);
        Assert.True(next.PlaylistDetails.GenresManuallyOverridden);
        Assert.Equal("Drama", next.PlaylistDetails.GenreText);
    }

    // --- Öffentlich-Flag ---

    /// <summary>Prüft die Öffentlich-Wache: nur Admin-Besitzer schalten um.</summary>
    [Fact]
    public void PlaylistPublicToggled_RequiresAdminOwner()
    {
        // Ohne Admin-Claim keine Umschaltung.
        var noAdmin = Connected() with { PlaylistDetails = OwnManualDetails() with { IsAdmin = false } };
        var (sameNoAdmin, noneNoAdmin) = AppUpdate.Update(
            noAdmin, new PlaylistPublicToggledMessage(true));
        Assert.Same(noAdmin, sameNoAdmin);
        Assert.Empty(noneNoAdmin);

        // Admin-Besitzer: Umschaltung läuft.
        var admin = Connected() with { PlaylistDetails = OwnManualDetails() with { IsAdmin = true } };
        var (_, effects) = AppUpdate.Update(admin, new PlaylistPublicToggledMessage(true));
        var effect = Assert.IsType<SetPlaylistPublicEffect>(Assert.Single(effects));
        Assert.True(effect.IsPublic);
    }

    /// <summary>Prüft das Öffentlich-Ergebnis: Erfolg übernimmt das Flag der Detailseite.</summary>
    [Fact]
    public void PlaylistPublicCompleted_Success_UpdatesFlag()
    {
        var model = Connected() with
        {
            PlaylistDetails = OwnManualDetails() with { IsAdmin = true, IsManaging = true }
        };
        var updated = Item(7) with { IsPublic = true };

        var (next, _) = AppUpdate.Update(model, new PlaylistPublicCompletedMessage(updated, null));

        Assert.True(next.PlaylistDetails.IsPublic);
        Assert.False(next.PlaylistDetails.IsManaging);
    }

    // --- Zu Playlist hinzufügen (Medien-Detailseite) ---

    private static AppModel WithMediaDetails()
    {
        return Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = "movie",
                MediaId = 42,
                Details = new MediaDetailsView
                {
                    MediaType = "movie",
                    MediaId = 42,
                    Title = "Film"
                }
            }
        };
    }

    /// <summary>Prüft die Anforderung: lädt die Ziel-Playlists einmalig.</summary>
    [Fact]
    public void PlaylistAddTargetsRequested_LoadsTargets()
    {
        var (_, effects) = AppUpdate.Update(WithMediaDetails(), new PlaylistAddTargetsRequestedMessage());

        Assert.IsType<LoadPlaylistTargetsEffect>(Assert.Single(effects));
    }

    /// <summary>Prüft den Ziel-Lauf: vorhandene Ziele eröffnen die Auswahl.</summary>
    [Fact]
    public void PlaylistTargetsLoaded_SetsTargets_KeepsPickerPending()
    {
        var model = WithMediaDetails() with
        {
            MediaDetails = WithMediaDetails().MediaDetails with
            {
                PlaylistPickerPending = true,
                IsLoadingPlaylistTargets = true
            }
        };

        var (next, _) = AppUpdate.Update(model,
            new PlaylistTargetsLoadedMessage([Item(9, "Liste A")], null));

        Assert.False(next.MediaDetails.IsLoadingPlaylistTargets);
        Assert.True(next.MediaDetails.PlaylistPickerPending);
        Assert.Single(next.MediaDetails.PlaylistTargets!);
    }

    /// <summary>Prüft die Auswahl: die gewählte Playlist startet den Hinzufüge-Effekt.</summary>
    [Fact]
    public void PlaylistPickerDismissed_Choice_EmitsAddEffect()
    {
        var target = Item(9, "Liste A");
        var model = WithMediaDetails() with
        {
            MediaDetails = WithMediaDetails().MediaDetails with
            {
                PlaylistPickerPending = true,
                PlaylistTargets = [target]
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistPickerDismissedMessage(9));

        Assert.True(next.MediaDetails.IsAddingToPlaylist);
        var effect = Assert.IsType<AddPlaylistEntryEffect>(Assert.Single(effects));
        Assert.Equal(9, effect.PlaylistId);
        Assert.Equal("Liste A", effect.PlaylistName);
        Assert.Equal("movie", effect.MediaType);
        Assert.Equal(42, effect.MediaId);
    }

    /// <summary>Prüft die Auswahl: Abbruch löst sich ohne Serveraufruf auf.</summary>
    [Fact]
    public void PlaylistPickerDismissed_Cancel_NoEffect()
    {
        var model = WithMediaDetails() with
        {
            MediaDetails = WithMediaDetails().MediaDetails with
            {
                PlaylistPickerPending = true,
                PlaylistTargets = [Item(9)]
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistPickerDismissedMessage(null));

        Assert.False(next.MediaDetails.PlaylistPickerPending);
        Assert.False(next.MediaDetails.IsAddingToPlaylist);
        Assert.Empty(effects);
    }

    /// <summary>Prüft das Hinzufüge-Ergebnis: der Hinweistext landet im Zustand.</summary>
    [Fact]
    public void PlaylistEntryAdded_SetsNotice()
    {
        var model = WithMediaDetails() with
        {
            MediaDetails = WithMediaDetails().MediaDetails with { IsAddingToPlaylist = true }
        };

        var (next, _) = AppUpdate.Update(model,
            new PlaylistEntryAddedMessage("Liste A", "Hinzugefügt.", null));

        Assert.False(next.MediaDetails.IsAddingToPlaylist);
        Assert.Equal("Hinzugefügt.", next.MediaDetails.AddToPlaylistNotice);
        Assert.False(next.MediaDetails.AddToPlaylistNoticeIsError);
    }
}
