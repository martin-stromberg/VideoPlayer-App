using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Controls;

/// <summary>
/// Geteiltes Dialog-Panel der Playlist-Anlage und -Bearbeitung: die
/// Seite klappt es über <see cref="ApplyModel(AppModel)"/> auf und
/// ab; alle Eingaben laufen als Messages in den MVU-Kreislauf. Der
/// <see cref="BindableObject.BindingContext"/> der Panel-Instanz ist
/// der <see cref="MvuHost"/> der beherbergenden Seite.
/// </summary>
public partial class PlaylistFormPanel : ContentView
{
    /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistFormPanel"/>-Klasse.</summary>
    public PlaylistFormPanel()
    {
        InitializeComponent();
    }

    private MvuHost? Host => BindingContext as MvuHost;

    /// <summary>Das Formular ist geöffnet (Sichtbarkeit des Panels).</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Ein Speichervorgang läuft (Felder/Schalter gesperrt).</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Der Sortiermodus ist wählbar (nur bei Neuanlage).</summary>
    public bool CanChooseSortMode { get; private set; }

    /// <summary>Inline-Fehlertext des Formulars.</summary>
    public string? Error { get; private set; }

    /// <summary>Ein Fehler liegt vor.</summary>
    public bool HasError { get; private set; }

    /// <summary>
    /// Übernimmt den Formular-Zustand aus dem App-Modell: Sichtbarkeit,
    /// Sperrung und Feldinhalte (nur bei Bedarf — ein laufender Tippvorgang
    /// darf den Eingabefokus nicht zurücksetzen).
    /// </summary>
    /// <param name="model">Das aktuelle App-Modell.</param>
    public void ApplyModel(AppModel model)
    {
        var form = model.PlaylistForm;
        var opened = form.IsOpen && !IsOpen;

        IsOpen = form.IsOpen;
        IsBusy = form.IsSaving;
        CanChooseSortMode = form.IsOpen && !form.IsEditing && !form.IsSaving;
        Error = form.Error;
        HasError = form.Error is not null;

        // Die Feldinhalte nur beim Öffnen oder bei externen Änderungen
        // setzen (z. B. leeres Formular der Neuanlage) — ein Tippen
        // liefert die Texte ohnehin schon in den Zustand.
        if (opened)
        {
            NameEntry.Text = form.Name;
            DescriptionEntry.Text = form.Description ?? string.Empty;
        }

        // Laufende Speichervorgänge sperren Eingaben und Schalter.
        NameEntry.IsEnabled = !form.IsSaving;
        DescriptionEntry.IsEnabled = !form.IsSaving;
        CancelButton.IsEnabled = !form.IsSaving;
        SaveButton.IsEnabled = !form.IsSaving;

        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanChooseSortMode));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
    }

    private void OnNameChanged(object? sender, TextChangedEventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistFormNameChangedMessage(e.NewTextValue ?? string.Empty));
    }

    private void OnDescriptionChanged(object? sender, TextChangedEventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistFormDescriptionChangedMessage(e.NewTextValue));
    }

    private void OnSortManualClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(
            new PlaylistFormSortModeSelectedMessage(ApiClient.Models.PlaylistSortModeValues.Manual));
    }

    private void OnSortByReleaseDateClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(
            new PlaylistFormSortModeSelectedMessage(ApiClient.Models.PlaylistSortModeValues.ByReleaseDate));
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistFormCancelMessage());
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistFormSaveRequestedMessage());
    }
}
