using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PictureInPictureService> logger;
    private bool isActive;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PictureInPictureService"/>-Klasse.</summary>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public PictureInPictureService(ILogger<PictureInPictureService> logger)
    {
        this.logger = logger;
    }

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
        // Nur eine tatsächlich erfolgte Umschaltung ist ein Ereignis —
        // ein verweigerter oder fehlgeschlagener Wechsel (Fenster nicht
        // greifbar, Ausnahme) wurde bereits als Warnung protokolliert.
        if (SetCompactOverlay(!isActive))
        {
            logger.LogInformation("PiP-Umschaltung: Mini-Player {State}.", isActive ? "aktiv" : "inaktiv");
        }
#endif
        return Task.FromResult(isActive);
    }

    /// <inheritdoc/>
    public Task RestoreAsync()
    {
#if WINDOWS
        if (isActive && SetCompactOverlay(false))
        {
            logger.LogInformation("PiP-Umschaltung: Vollfenster wiederhergestellt.");
        }
#endif
        return Task.CompletedTask;
    }

#if WINDOWS
    /// <summary>
    /// Schaltet den Presenter des App-Fensters zwischen kompaktem
    /// Overlay und Vollfenster um.
    /// </summary>
    /// <param name="active"><c>true</c> für das kompakte Overlay, <c>false</c> für das Vollfenster.</param>
    /// <returns><c>true</c>, wenn der Presenter tatsächlich umgeschaltet wurde; <c>false</c> bei verweigertem/fehlgeschlagenem Wechsel (bereits als Warnung protokolliert).</returns>
    private bool SetCompactOverlay(bool active)
    {
        try
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            var platformWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (platformWindow?.AppWindow is not { } appWindow)
            {
                logger.LogWarning("PiP-Umschaltung nicht möglich — das Fenster ist nicht greifbar.");
                return false;
            }

            appWindow.SetPresenter(active
                ? Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay
                : Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
            isActive = active;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PiP-Umschalten fehlgeschlagen.");
            return false;
        }
    }
#endif
}
