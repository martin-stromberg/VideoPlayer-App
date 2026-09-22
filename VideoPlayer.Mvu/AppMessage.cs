using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>Basistyp aller MVU-Nachrichten der App.</summary>
public abstract record AppMessage;

/// <summary>Start-Workflow anstoßen (Erreichbarkeitsprüfung).</summary>
public sealed record StartupRequestedMessage : AppMessage;

/// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
public sealed record ServerHealthCheckCompletedMessage(bool IsReachable) : AppMessage;

/// <summary>Programmatische Navigation zu einer Menüseite anfordern.</summary>
public sealed record NavigateToMessage(AppPage Page) : AppMessage;

/// <summary>Die Shell hat eine Navigation abgeschlossen (Model synchronisieren).</summary>
public sealed record NavigatedMessage(AppPage Page) : AppMessage;

/// <summary>Einstellungen aus der Ablage (erneut) laden anfordern.</summary>
public sealed record LoadSettingsRequestedMessage : AppMessage;

/// <summary>Einstellungen wurden aus der Ablage geladen.</summary>
public sealed record SettingsLoadedMessage(AppSettings Settings) : AppMessage;

/// <summary>Geänderte Einstellungen übernehmen und persistent speichern.</summary>
public sealed record SaveSettingsRequestedMessage(AppSettings Settings) : AppMessage;
