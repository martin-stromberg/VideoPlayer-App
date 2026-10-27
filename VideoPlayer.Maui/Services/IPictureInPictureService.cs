namespace VideoPlayer.Maui.Services;

/// <summary>
/// Picture-in-Picture-/Mini-Player-Umschaltung der Wiedergabeseite
/// (plattformbezogener Dienst am App-Rand). Auf iOS steht PiP über die
/// native Transportsteuerung des <c>AVPlayerViewController</c> bereits
/// zur Verfügung — dort ist <see cref="IsToggleSupported"/> <c>false</c>
/// und kein eigener Schalter nötig; auf Windows schaltet der Dienst das
/// App-Fenster in den kompakten Overlay-Modus (Mini-Player) und zurück.
/// </summary>
public interface IPictureInPictureService
{
    /// <summary>
    /// Die Plattform bietet eine eigene Umschaltung an (Windows) — die
    /// Seite zeigt dann einen Mini-Player-Schalter.
    /// </summary>
    bool IsToggleSupported { get; }

    /// <summary>Der kompakte Overlay-Modus ist gerade aktiv.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Schaltet den kompakten Overlay-Modus um (aktiv ↔ normal).
    /// </summary>
    /// <returns><c>true</c>, wenn der Overlay-Modus danach aktiv ist.</returns>
    Task<bool> ToggleAsync();

    /// <summary>
    /// Stellt den normalen Fenstermodus wieder her, falls der
    /// Overlay-Modus aktiv ist (z. B. beim Verlassen der Seite).
    /// </summary>
    /// <returns>Eine abgeschlossene Aufgabe nach der Wiederherstellung.</returns>
    Task RestoreAsync();
}
