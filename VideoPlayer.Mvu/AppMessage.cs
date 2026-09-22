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
