using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Controls;

/// <summary>
/// Dialog-Panel des Playlist-Genre-Editors: zeigt die vom Server
/// geladenen Genre-Optionen als auswählbare Chip-Zeilen (Klartext-Namen)
/// und reicht Speichern/Zurücksetzen/Abbrechen als Messages in den
/// MVU-Kreislauf. Der <see cref="BindableObject.BindingContext"/> der
/// Panel-Instanz ist der <see cref="MvuHost"/> der beherbergenden Seite.
/// </summary>
public partial class PlaylistGenreEditorPanel : ContentView
{
    /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistGenreEditorPanel"/>-Klasse.</summary>
    public PlaylistGenreEditorPanel()
    {
        InitializeComponent();
    }

    private MvuHost? Host => BindingContext as MvuHost;

    /// <summary>Der Editor ist geöffnet (Sichtbarkeit des Panels).</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Die Genre-Optionen werden geladen.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Wählbare Optionen liegen vor.</summary>
    public bool HasOptions { get; private set; }

    /// <summary>Der Editor ist fertig geladen, aber ohne Optionen.</summary>
    public bool ShowEmpty { get; private set; }

    /// <summary>Inline-Fehlertext des Editors.</summary>
    public string? Error { get; private set; }

    /// <summary>Ein Fehler liegt vor.</summary>
    public bool HasError { get; private set; }

    /// <summary>
    /// Übernimmt den Editor-Zustand aus dem App-Modell: Sichtbarkeit,
    /// Optionen (als Chip-Zeilen mit AutomationId <c>GenreOption_{id}</c>)
    /// und Schalter-Sperrung.
    /// </summary>
    /// <param name="model">Das aktuelle App-Modell.</param>
    public void ApplyModel(AppModel model)
    {
        var editor = model.PlaylistGenreEditor;
        IsOpen = editor.IsOpen;
        IsLoading = editor.IsLoading;
        HasOptions = editor.Options.Count > 0;
        ShowEmpty = editor.IsOpen && !editor.IsLoading && editor.Options.Count == 0 && editor.Error is null;
        Error = editor.Error;
        HasError = editor.Error is not null;

        // Die Optionen liegen nur gelesen im Zustand — die Chip-Zeilen
        // werden aus dem Modell neu aufgebaut (kleine Listen, einfachste
        // Aktualisierung); der Auswahlzustand steht je Zeile im Text.
        if (editor.IsOpen && !editor.IsLoading)
        {
            RebuildOptions(editor.Options);
        }

        SaveButton.IsEnabled = editor.IsOpen && !editor.IsLoading && !editor.IsSaving;
        ResetButton.IsEnabled = editor.IsOpen && !editor.IsLoading && !editor.IsSaving
            && model.PlaylistDetails.GenresManuallyOverridden;
        CancelButton.IsEnabled = editor.IsOpen && !editor.IsSaving;

        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasOptions));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
    }

    private void RebuildOptions(IReadOnlyList<PlaylistGenreOptionItem> options)
    {
        OptionsHost.Children.Clear();
        foreach (var option in options)
        {
            var chip = new Border
            {
                AutomationId = option.OptionAutomationId,
                Padding = new Thickness(10, 6),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
            };
            if (Application.Current?.Resources.TryGetValue("VpBorder", out var stroke) == true
                && stroke is Color strokeColor)
            {
                chip.Stroke = strokeColor;
            }

            var label = new Label
            {
                Text = option.IsSelected ? $"✓ {option.Name}" : option.Name,
                FontSize = 14
            };
            if (Application.Current?.Resources.TryGetValue("VpText", out var text) == true
                && text is Color textColor)
            {
                label.TextColor = textColor;
            }

            chip.Content = label;
            chip.GestureRecognizers.Add(new TapGestureRecognizer
            {
                CommandParameter = option.Id,
                Command = new Command<long>(id =>
                    _ = Host?.DispatchAsync(new PlaylistGenreToggledMessage(id)))
            });
            OptionsHost.Children.Add(chip);
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistGenresSaveRequestedMessage());
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistGenresResetRequestedMessage());
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _ = Host?.DispatchAsync(new PlaylistGenreEditorCancelMessage());
    }
}
