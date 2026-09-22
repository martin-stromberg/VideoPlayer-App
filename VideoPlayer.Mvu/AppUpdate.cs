using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Reine Update-Funktion des MVU-Zyklus: bildet <see cref="AppMessage"/> auf ein
/// neues <see cref="AppModel"/> plus auszuführende <see cref="AppEffect"/>s ab.
/// </summary>
public static class AppUpdate
{
    /// <summary>Fehlertext bei ungültiger manueller Adresseingabe.</summary>
    public const string InvalidAddressError = "Bitte eine gültige Serveradresse eingeben.";

    /// <summary>Fehlertext bei fehlgeschlagener Verbindungsprüfung.</summary>
    public const string UnreachableServerError = "Der Server ist nicht erreichbar. Bitte die Adresse prüfen.";

    public static AppTransition Update(AppModel model, AppMessage message)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(message);

        return message switch
        {
            StartupRequestedMessage => UpdateStartupRequested(model),
            ServerHealthCheckCompletedMessage completed => UpdateHealthCheckCompleted(model, completed),
            NavigateToMessage navigate => UpdateNavigateTo(model, navigate),
            NavigatedMessage navigated => UpdateNavigated(model, navigated),
            LoadSettingsRequestedMessage => new AppTransition(model, [new LoadSettingsEffect()]),
            SettingsLoadedMessage loaded => UpdateSettingsLoaded(model, loaded),
            SaveSettingsRequestedMessage save => UpdateSaveSettingsRequested(model, save),
            LoadServersRequestedMessage => new AppTransition(model, [new LoadServersEffect()]),
            ServersLoadedMessage loaded => UpdateServersLoaded(model, loaded),
            ServerDiscoveryRequestedMessage => UpdateDiscoveryRequested(model),
            ServerDiscoveryCompletedMessage discovered => UpdateDiscoveryCompleted(model, discovered),
            ServerAddressSubmittedMessage submitted => UpdateAddressSubmitted(model, submitted),
            DiscoveredServerSelectedMessage selected => UpdateServerCandidateSelected(model, selected.Address, selected.DisplayName),
            ServerSwitchRequestedMessage switchTo => UpdateServerCandidateSelected(model, switchTo.Address, switchTo.DisplayName),
            ServerCheckCompletedMessage check => UpdateServerCheckCompleted(model, check),
            _ => AppTransition.Unchanged(model)
        };
    }

    private static AppTransition UpdateStartupRequested(AppModel model)
    {
        if (model.IsStartupRunning && model.ServerHealth == ServerHealthStatus.Checking)
        {
            return AppTransition.Unchanged(model);
        }

        // Der Health-Check folgt erst nach dem Laden der Serververwaltung —
        // ohne hinterlegten Server verzweigt der Start in die Einrichtung.
        return new AppTransition(
            model with { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking },
            [new InitializeStorageEffect(), new LoadSettingsEffect(), new LoadServersEffect()]);
    }

    private static AppTransition UpdateSettingsLoaded(AppModel model, SettingsLoadedMessage message)
    {
        return new AppTransition(
            model with { Settings = message.Settings.Normalize() },
            []);
    }

    private static AppTransition UpdateSaveSettingsRequested(AppModel model, SaveSettingsRequestedMessage message)
    {
        var normalized = message.Settings.Normalize();
        return new AppTransition(
            model with { Settings = normalized },
            [new PersistSettingsEffect(normalized)]);
    }

    private static AppTransition UpdateServersLoaded(AppModel model, ServersLoadedMessage message)
    {
        var updated = model with
        {
            KnownServers = message.Servers,
            ActiveServerAddress = message.ActiveAddress
        };

        if (message.ActiveAddress is not null)
        {
            // Hinterlegter Server: beim Start Erreichbarkeit prüfen (Abschluss
            // navigiert zur Startseite). Beim Nachladen aus der geöffneten
            // Serververwaltung („Server wechseln") nur das Model aktualisieren.
            return model.IsStartupRunning
                ? new AppTransition(updated, [new CheckServerHealthEffect()])
                : new AppTransition(updated, []);
        }

        // Erststart ohne hinterlegten Server: Einrichtungsdialog anzeigen.
        updated = updated with
        {
            IsStartupRunning = false,
            ServerHealth = ServerHealthStatus.Unknown,
            CurrentPage = AppPage.ServerSetup
        };

        return model.CurrentPage == AppPage.ServerSetup
            ? new AppTransition(updated, [])
            : new AppTransition(updated, [new NavigateToPageEffect(AppPage.ServerSetup)]);
    }

    private static AppTransition UpdateDiscoveryRequested(AppModel model)
    {
        if (model.IsDiscoveryRunning)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { IsDiscoveryRunning = true },
            [new DiscoverServersEffect()]);
    }

    private static AppTransition UpdateDiscoveryCompleted(AppModel model, ServerDiscoveryCompletedMessage message)
    {
        return new AppTransition(
            model with
            {
                IsDiscoveryRunning = false,
                DiscoveryAttempted = true,
                DiscoveredServers = message.Servers
            },
            []);
    }

    private static AppTransition UpdateAddressSubmitted(AppModel model, ServerAddressSubmittedMessage message)
    {
        if (!ServerAddress.TryNormalize(message.Text, out var normalized))
        {
            return new AppTransition(
                model with { ServerSetupError = InvalidAddressError },
                []);
        }

        return BeginServerCheck(model, normalized, null);
    }

    private static AppTransition UpdateServerCandidateSelected(AppModel model, string address, string? displayName)
    {
        if (!ServerAddress.TryNormalize(address, out var normalized))
        {
            return new AppTransition(
                model with { ServerSetupError = InvalidAddressError },
                []);
        }

        return BeginServerCheck(model, normalized, displayName);
    }

    private static AppTransition BeginServerCheck(AppModel model, string normalizedAddress, string? displayName)
    {
        return new AppTransition(
            model with { IsServerCheckRunning = true, ServerSetupError = null },
            [new CheckCandidateServerEffect(normalizedAddress, displayName)]);
    }

    private static AppTransition UpdateServerCheckCompleted(AppModel model, ServerCheckCompletedMessage message)
    {
        if (!message.Success)
        {
            return new AppTransition(
                model with
                {
                    IsServerCheckRunning = false,
                    ServerSetupError = UnreachableServerError
                },
                []);
        }

        var normalized = ServerAddress.Normalize(message.Address);
        var server = new KnownServer
        {
            Address = normalized,
            DisplayName = message.DisplayName ?? string.Empty,
            LastUsedAtUtc = DateTime.UtcNow
        };

        var known = model.KnownServers
            .Where(s => !string.Equals(s.Address, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        known.Insert(0, server);

        return new AppTransition(
            model with
            {
                IsServerCheckRunning = false,
                ServerSetupError = null,
                ActiveServerAddress = normalized,
                KnownServers = known,
                ServerHealth = ServerHealthStatus.Reachable,
                IsStartupRunning = false,
                CurrentPage = AppPage.Home
            },
            [new PersistActiveServerEffect(normalized, message.DisplayName), new NavigateToPageEffect(AppPage.Home)]);
    }

    private static AppTransition UpdateHealthCheckCompleted(AppModel model, ServerHealthCheckCompletedMessage message)
    {
        return new AppTransition(
            model with
            {
                IsStartupRunning = false,
                ServerHealth = message.IsReachable ? ServerHealthStatus.Reachable : ServerHealthStatus.Unreachable,
                CurrentPage = AppPage.Home
            },
            [new NavigateToPageEffect(AppPage.Home)]);
    }

    private static AppTransition UpdateNavigateTo(AppModel model, NavigateToMessage message)
    {
        if (message.Page == AppPage.Loading || message.Page == model.CurrentPage)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { CurrentPage = message.Page },
            [new NavigateToPageEffect(message.Page)]);
    }

    private static AppTransition UpdateNavigated(AppModel model, NavigatedMessage message)
    {
        if (message.Page == AppPage.Loading || message.Page == model.CurrentPage)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(model with { CurrentPage = message.Page }, []);
    }
}
