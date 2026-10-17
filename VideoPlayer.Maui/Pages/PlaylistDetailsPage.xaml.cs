using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Detailseite einer Playlist (Shell-Route „playlistdetails?id=…&amp;name=…"):
/// Kopfbereich mit Cover, Name, Beschreibung, Sortiermodus, Genres und
/// Eintragszahl; darunter die Einträge in der serverseitigen Reihenfolge
/// mit seitenweisem Nachladen (<c>api/playlists/{id}/entries/paged</c>).
/// Bei eigener Playlist („Verwalten" bzw. der ⋮-Kontextschalter je
/// Eintrag) öffnet die Seite seiten-nah die Verwaltungsaktionen:
/// Bearbeiten, Löschen, Sortiermodus-Wechsel, Genre-Editor,
/// Öffentlich-Flag (Administrator) sowie Entfernen und Verschieben der
/// Einträge — jede Anfrage läuft als Message in den MVU-Kreislauf;
/// Bestätigungskonflikte (HTTP 409) öffnet die Seite einmalig als
/// DisplayAlertAsync-Dialog.
/// </summary>
[QueryProperty(nameof(PlaylistId), "id")]
[QueryProperty(nameof(PlaylistName), "name")]
public partial class PlaylistDetailsPage : ContentPage
{
    private const string CancelText = "Abbrechen";

    private readonly MvuHost mvuHost;
    private long playlistId;
    private string? playlistName;
    private bool subscribed;

    /// <summary>Bereits angezeigter Weiterschauen-Konflikt (Dialog nur einmal pro Konflikt-Zustand).</summary>
    private long? shownRemoveConflictId;

    /// <summary>Bereits angezeigter Sortiermodus-Konflikt (Dialog nur einmal pro Konflikt-Zustand).</summary>
    private string? shownSortModeConflict;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistDetailsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Playlist-Nachrichten.</param>
    public PlaylistDetailsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        UpdateUi(mvuHost.Model);
    }

    /// <summary>Playlist-Id aus dem Navigationspfad.</summary>
    public string PlaylistId
    {
        set => playlistId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) ? id : 0;
    }

    /// <summary>Playlist-Name aus dem Navigationspfad (Seitentitel-Fallback).</summary>
    public string PlaylistName
    {
        set
        {
            playlistName = Uri.UnescapeDataString(value ?? string.Empty);
            Title = string.IsNullOrWhiteSpace(playlistName) ? "Playlist" : playlistName;
        }
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Das ModelChanged-Abo gilt nur, solange die Seite sichtbar ist
        // (Muster: SettingsPage) — verlassene Instanzen dürfen nicht
        // weiter am MvuHost hängen.
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
        // Bei geöffneter Playlist ohne Zustand (Wiederherstellungspfad)
        // werden Kopfdaten und erste Eintragsseite geladen; bei Rückkehr
        // auf dieselbe Playlist bleibt der Stand erhalten.
        if (mvuHost.Model.PlaylistDetails.PlaylistId != playlistId || !mvuHost.Model.PlaylistDetails.Attempted)
        {
            _ = mvuHost.DispatchAsync(new PlaylistDetailsOpenedMessage(playlistId, playlistName ?? string.Empty));
        }
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        subscribed = false;
        mvuHost.ModelChanged -= OnModelChanged;
        base.OnDisappearing();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateUi(model);
            }
        });
    }

    private void UpdateUi(AppModel model)
    {
        var details = model.PlaylistDetails;
        ErrorPanel.IsVisible = details.HasError && details.Entries.Count == 0;
        ErrorLabel.Text = details.Error;
        FooterErrorPanel.IsVisible = details.HasError && details.Entries.Count > 0;
        EmptyLabel.IsVisible = details.ShowEmpty;
        EntriesCollection.IsVisible = !details.ShowEmpty;
        if (details.Name.Length > 0)
        {
            Title = details.Name;
        }

        // Hinweiszeile der Verwaltungsoperationen (Erfolg und Fehler).
        ManageNoticeLabel.IsVisible = details.ManageNotice is not null;
        ManageNoticeLabel.Text = details.ManageNotice;

        PlaylistFormPanel.ApplyModel(model);
        PlaylistGenreEditorPanel.ApplyModel(model);

        // Ausstehende Konflikte einmalig als Bestätigungsdialog zeigen;
        // löst sich der Konflikt auf, wird die Sperre zurückgesetzt.
        if (details.PendingRemoveConflict is { } pending)
        {
            if (shownRemoveConflictId != pending.EntryId)
            {
                shownRemoveConflictId = pending.EntryId;
                _ = ShowRemoveConflictAsync(pending);
            }
        }
        else
        {
            shownRemoveConflictId = null;
        }

        if (details.PendingSortModeConflict is { } pendingSortMode)
        {
            if (!string.Equals(shownSortModeConflict, pendingSortMode, StringComparison.Ordinal))
            {
                shownSortModeConflict = pendingSortMode;
                _ = ShowSortModeConflictAsync(pendingSortMode);
            }
        }
        else
        {
            shownSortModeConflict = null;
        }
    }

    /// <summary>
    /// Bestätigungsdialog des Weiterschauen-Konflikts (HTTP 409 beim
    /// Eintrags-Entfernen): die Entscheidung läuft als
    /// <see cref="PlaylistEntryRemoveConfirmedMessage"/> zurück in den
    /// MVU-Kreislauf.
    /// </summary>
    /// <param name="entry">Der vom Konflikt betroffene Eintrag.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Dialog-Entscheidung.</returns>
    private async Task ShowRemoveConflictAsync(PlaylistEntryItem entry)
    {
        var confirmed = await DisplayAlertAsync(
            "Aus Weiterschauen entfernen?",
            $"„{entry.Title}“ steht in „Weiterschauen“. Das Entfernen aus der Playlist löscht den Eintrag dort ebenfalls.",
            "Entfernen",
            CancelText);
        _ = mvuHost.DispatchAsync(new PlaylistEntryRemoveConfirmedMessage(confirmed));
    }

    /// <summary>
    /// Bestätigungsdialog des Sortiermodus-Konflikts (HTTP 409): der
    /// Wechsel verwirft die manuelle Reihenfolge.
    /// </summary>
    /// <param name="newSortMode">Der angeforderte Ziel-Sortiermodus.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Dialog-Entscheidung.</returns>
    private async Task ShowSortModeConflictAsync(string newSortMode)
    {
        var confirmed = await DisplayAlertAsync(
            "Sortiermodus ändern?",
            "Der Wechsel des Sortiermodus verwirft die manuelle Reihenfolge der Einträge.",
            "Ändern",
            CancelText);
        _ = mvuHost.DispatchAsync(new PlaylistSortModeConfirmedMessage(confirmed));
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        // „Abspielen" ohne Startvorgabe: der Server beginnt beim ersten
        // abspielbaren und zugänglichen Eintrag.
        _ = mvuHost.DispatchAsync(new PlaylistPlaybackStartRequestedMessage(playlistId, null));
    }

    /// <summary>
    /// Verwaltungsmenü der geöffneten Playlist („Verwalten"): die
    /// Klartext-Aktionen hängen vom Zustand ab — der Öffentlich-Schalter
    /// steht nur Administrator:innen mit eigener Playlist, der
    /// Löschpunkt fordert eine eigene Bestätigung.
    /// </summary>
    /// <param name="sender">Der auslösende Schalter.</param>
    /// <param name="e">Die Ereignisdaten.</param>
    private async void OnManageClicked(object? sender, EventArgs e)
    {
        var details = mvuHost.Model.PlaylistDetails;
        if (!details.CanManage || details.IsManaging)
        {
            return;
        }

        const string editText = "Bearbeiten";
        const string genreText = "Genres bearbeiten";
        const string deleteText = "Playlist löschen";
        var sortModeText = details.SortMode == ApiClient.Models.PlaylistSortModeValues.Manual
            ? "Nach Erscheinungsdatum sortieren"
            : "Manuell sortieren";
        var publicText = details.IsPublic ? "Privat schalten" : "Öffentlich schalten";

        var actions = new List<string> { editText, sortModeText, genreText };
        if (details.CanSetPublic)
        {
            actions.Add(publicText);
        }

        actions.Add(deleteText);

        var choice = await DisplayActionSheetAsync(
            "Playlist verwalten", CancelText, null, actions.ToArray());
        switch (choice)
        {
            case editText:
                _ = mvuHost.DispatchAsync(new PlaylistEditRequestedMessage());
                break;
            case genreText:
                _ = mvuHost.DispatchAsync(new PlaylistGenreEditorRequestedMessage());
                break;
            case deleteText:
                await ConfirmDeleteAsync(details);
                break;
            default:
                if (choice == sortModeText)
                {
                    var newMode = details.SortMode == ApiClient.Models.PlaylistSortModeValues.Manual
                        ? ApiClient.Models.PlaylistSortModeValues.ByReleaseDate
                        : ApiClient.Models.PlaylistSortModeValues.Manual;
                    _ = mvuHost.DispatchAsync(new PlaylistSortModeRequestedMessage(newMode));
                }
                else if (choice == publicText)
                {
                    _ = mvuHost.DispatchAsync(new PlaylistPublicToggledMessage(!details.IsPublic));
                }

                break;
        }
    }

    /// <summary>
    /// Bestätigungsdialog des Playlist-Löschens: die Entscheidung läuft
    /// als <see cref="PlaylistDeleteConfirmedMessage"/> in den
    /// MVU-Kreislauf.
    /// </summary>
    /// <param name="details">Der Detail-Zustand der Playlist.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Dialog-Entscheidung.</returns>
    private async Task ConfirmDeleteAsync(PlaylistDetailsState details)
    {
        var confirmed = await DisplayAlertAsync(
            "Playlist löschen?",
            $"„{details.Name}“ wird dauerhaft gelöscht.",
            "Löschen",
            CancelText);
        if (confirmed)
        {
            _ = mvuHost.DispatchAsync(new PlaylistDeleteConfirmedMessage(details.PlaylistId));
        }
    }

    /// <summary>
    /// Kontextmenü eines Eintrags (⋮-Schalter): im manuellen
    /// Sortiermodus die Verschiebe-Aktionen (Ränder jeweils ausgeblendet),
    /// immer das Entfernen — jede Wahl läuft als Message in den
    /// MVU-Kreislauf.
    /// </summary>
    /// <param name="sender">Der Menü-Schalter des Eintrags (trägt den Eintrag im BindingContext).</param>
    /// <param name="e">Die Ereignisdaten.</param>
    private async void OnEntryMenuClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: PlaylistEntryItem entry })
        {
            return;
        }

        var details = mvuHost.Model.PlaylistDetails;
        if (!details.CanManage || details.IsManaging)
        {
            return;
        }

        const string beginningText = "An den Anfang";
        const string endText = "Ans Ende";
        const string forwardText = "Eins nach vorn";
        const string backwardText = "Eins nach hinten";
        const string removeText = "Aus Playlist entfernen";

        var actions = new List<string>();
        if (details.CanReorder)
        {
            if (!details.IsFirstEntry(entry))
            {
                actions.Add(beginningText);
                actions.Add(forwardText);
            }

            if (!details.IsLastEntry(entry))
            {
                actions.Add(backwardText);
                actions.Add(endText);
            }
        }

        actions.Add(removeText);

        var choice = await DisplayActionSheetAsync(
            entry.Title, CancelText, null, actions.ToArray());
        switch (choice)
        {
            case beginningText:
                _ = mvuHost.DispatchAsync(new PlaylistEntryMoveToBeginningRequestedMessage(entry));
                break;
            case endText:
                _ = mvuHost.DispatchAsync(new PlaylistEntryMoveToEndRequestedMessage(entry));
                break;
            case forwardText:
                if (details.NeighborSortOrder(entry, forward: false) is { } previousOrder)
                {
                    _ = mvuHost.DispatchAsync(
                        new PlaylistEntryMoveBetweenRequestedMessage(entry, previousOrder));
                }

                break;
            case backwardText:
                if (details.NeighborSortOrder(entry, forward: true) is { } nextOrder)
                {
                    _ = mvuHost.DispatchAsync(
                        new PlaylistEntryMoveBetweenRequestedMessage(entry, nextOrder));
                }

                break;
            case removeText:
                _ = mvuHost.DispatchAsync(new PlaylistEntryRemoveRequestedMessage(entry));
                break;
        }
    }

    private void OnEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PlaylistEntryItem item)
        {
            _ = mvuHost.DispatchAsync(new PlaylistEntrySelectedMessage(item));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistEntriesNextPageRequestedMessage());
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistEntriesRetryRequestedMessage());
    }
}
