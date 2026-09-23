namespace VideoPlayer.Maui.Services;

/// <summary>
/// Windows-Implementierung der PiP-Umschaltung: schaltet den Presenter
/// des App-Fensters zwischen <c>CompactOverlay</c> (immer im Vordergrund
/// schwebender Mini-Player) und <c>Default</c> um — die „kompakte
/// Overlay-/Mini-Player-Fenster"-Anforderung der Windows-Plattform.
/// Auf allen anderen Plattformen ist der Dienst inaktiv (iOS nutzt den
/// nativen PiP-Modus der MediaElement-Transportsteuerung).
/// </summary>
public sealed class PictureInPictureService : IPictureInPictureService
{
    private bool isActive;

    /// <inheritdoc/>
    public bool IsToggleSupported =>
#if WINDOWS
        true;
#else
        false;
#endif

    /// <inheritdoc/>
    public bool IsActive => isActive;

    /// <inheritdoc/>
    public Task<bool> ToggleAsync()
    {
#if WINDOWS
        SetCompactOverlay(!isActive);
#endif
        return Task.FromResult(isActive);
    }

    /// <inheritdoc/>
    public Task RestoreAsync()
    {
#if WINDOWS
        if (isActive)
        {
            SetCompactOverlay(false);
        }
#endif
        return Task.CompletedTask;
    }

#if WINDOWS
    private void SetCompactOverlay(bool active)
    {
        try
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            var platformWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (platformWindow?.AppWindow is not { } appWindow)
            {
                return;
            }

            appWindow.SetPresenter(active
                ? Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay
                : Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
            isActive = active;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PictureInPictureService] Umschalten fehlgeschlagen: {ex.Message}");
        }
    }
#endif
}
