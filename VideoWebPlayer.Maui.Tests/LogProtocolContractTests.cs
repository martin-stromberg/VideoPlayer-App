using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Protokoll-Features (Issue #43, Schritt 3): die
/// Verdrahtung der Einstellungsseite, der Effekt-Handler des
/// Protokollversands, die DI-Registrierung und die Developer-API-Felder
/// werden über die Quelldateien gepinnt, weil das Testprojekt die
/// MAUI-Targets nicht referenzieren kann.
/// </summary>
public class LogProtocolContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string ReadFile(params string[] parts)
        => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Die Einstellungsseite trägt den Protokoll-Abschnitt mit Schalter, Versand-Bereich und Fehlerlabel.</summary>
    [Fact]
    public void SettingsPage_Xaml_ContainsLoggingSection()
    {
        var xaml = ReadFile("VideoPlayer.Maui", "Pages", "SettingsPage.xaml");

        Assert.Contains("Text=\"Protokoll\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Protokollierung\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationId=\"LoggingSwitch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SendLogSection\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationId=\"SendLogButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Protokoll senden\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnSendLogClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationId=\"SendLogErrorLabel\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Der Code-Behind verdrahtet Schalter und Versand-Button auf die MVU-Nachrichten und befüllt die Sichtbarkeit aus dem Modell.</summary>
    [Fact]
    public void SettingsPage_CodeBehind_WiresLoggingControls()
    {
        var source = ReadFile("VideoPlayer.Maui", "Pages", "SettingsPage.xaml.cs");

        Assert.Contains("LoggingSwitch.Toggled += OnLoggingToggled", source, StringComparison.Ordinal);
        Assert.Contains("s with { LoggingEnabled = e.Value }", source, StringComparison.Ordinal);
        Assert.Contains("SendLogProtocolRequestedMessage", source, StringComparison.Ordinal);
        Assert.Contains("SendLogSection.IsVisible = settings.LoggingEnabled", source, StringComparison.Ordinal);
        Assert.Contains("SendLogErrorLabel.IsVisible = mvuHost.Model.LogSendError is not null", source, StringComparison.Ordinal);
    }

    /// <summary>Der Effekt-Handler behandelt den Versand-Effekt über Mail mit Share-Fallback und dem festen Empfänger.</summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesSendLogEffect_ViaEmailAndShareFallback()
    {
        var source = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("case SendLogProtocolEffect:", source, StringComparison.Ordinal);
        Assert.Contains("SendLogProtocolAsync", source, StringComparison.Ordinal);
        Assert.Contains("Email.ComposeAsync", source, StringComparison.Ordinal);
        Assert.Contains("EmailAttachment", source, StringComparison.Ordinal);
        Assert.Contains("mstromberg84+heimkino@gmail.com", source, StringComparison.Ordinal);
        Assert.Contains("Share.RequestAsync", source, StringComparison.Ordinal);
        Assert.Contains("ShareFileRequest", source, StringComparison.Ordinal);
        Assert.Contains("MainThread.InvokeOnMainThreadAsync", source, StringComparison.Ordinal);
        Assert.Contains("SendLogProtocolCompletedMessage", source, StringComparison.Ordinal);
        Assert.Contains("appLog.SetEnabled", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Diagnostics.Debug.WriteLine", source, StringComparison.Ordinal);
    }

    /// <summary>Die MauiProgram-Registrierung verdrahtet Protokolldienst und Provider mit dem Einstellungs-Startwert.</summary>
    [Fact]
    public void MauiProgram_RegistersLogServices()
    {
        var source = ReadFile("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("AddSingleton(sp =>", source, StringComparison.Ordinal);
        Assert.Contains("new AppLogService(", source, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<AppStorageLayout>()", source, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<AppSettingsService>().Load().LoggingEnabled", source, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<ILoggerProvider>", source, StringComparison.Ordinal);
        Assert.Contains("new FileLoggerProvider(sp.GetRequiredService<AppLogService>())", source, StringComparison.Ordinal);
    }

    /// <summary>Der Developer-API-Snapshot führt die Protokoll-Felder für die E2E-Beobachtbarkeit.</summary>
    [Fact]
    public void DevApiSnapshot_ExposesLoggingFields()
    {
        var snapshot = ReadFile("VideoPlayer.Mvu", "DevApi", "DevApiModelSnapshot.cs");

        Assert.Contains("SettingsLoggingEnabled = model.Settings.LoggingEnabled", snapshot, StringComparison.Ordinal);
        Assert.Contains("LogSendError = model.LogSendError", snapshot, StringComparison.Ordinal);

        var e2eModel = ReadFile("VideoPlayer.E2E.Tests", "Support", "DevApiModels.cs");
        Assert.Contains("\"settingsLoggingEnabled\"", e2eModel, StringComparison.Ordinal);
        Assert.Contains("\"logSendError\"", e2eModel, StringComparison.Ordinal);
    }

    /// <summary>Der Update-Schalter kennt Guard und Fehlertexte des Versands.</summary>
    [Fact]
    public void AppUpdate_GuardsSendLogProtocol()
    {
        var source = ReadFile("VideoPlayer.Mvu", "AppUpdate.cs");

        Assert.Contains("SendLogProtocolRequestedMessage => UpdateSendLogProtocolRequested(model)", source, StringComparison.Ordinal);
        Assert.Contains("SendLogProtocolCompletedMessage completed => UpdateSendLogProtocolCompleted(model, completed)", source, StringComparison.Ordinal);
        Assert.Contains("!model.Settings.LoggingEnabled", source, StringComparison.Ordinal);
        Assert.Contains("new SendLogProtocolEffect()", source, StringComparison.Ordinal);
        Assert.Contains("LogProtocolEmptyError", source, StringComparison.Ordinal);
        Assert.Contains("LogSendFailedError", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Netzstatus-Monitor protokolliert den Online-/Offline-Wechsel
    /// per injiziertem Logger (einzige Schicht — der Effekt-Handler
    /// beschränkt sich auf den Dispatch, kein zweiter Eintrag).
    /// </summary>
    [Fact]
    public void ConnectivityMonitor_LogsAvailabilityChange()
    {
        var monitor = ReadFile("VideoPlayer.Maui", "Services", "ConnectivityNetworkStatusMonitor.cs");

        Assert.Contains("ILogger<ConnectivityNetworkStatusMonitor>", monitor, StringComparison.Ordinal);
        Assert.Contains("LogInformation", monitor, StringComparison.Ordinal);
        Assert.Contains("AvailabilityChanged?.Invoke", monitor, StringComparison.Ordinal);

        var handler = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");
        var body = SliceMethod(handler, "private void OnNetworkAvailabilityChanged");
        Assert.DoesNotContain("LogInformation", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der PiP-Dienst protokolliert Umschalten und Wiederherstellen;
    /// der Effekt-Handler schreibt den PiP-Breadcrumb im
    /// Toggle-Pfad.
    /// </summary>
    [Fact]
    public void PictureInPictureService_LogsToggleRestore()
    {
        var pip = ReadFile("VideoPlayer.Maui", "Services", "PictureInPictureService.cs");

        Assert.Contains("ILogger<PictureInPictureService>", pip, StringComparison.Ordinal);
        var toggleBody = SliceMethod(pip, "public Task<bool> ToggleAsync()");
        Assert.Contains("LogInformation", toggleBody, StringComparison.Ordinal);
        var restoreBody = SliceMethod(pip, "public Task RestoreAsync()");
        Assert.Contains("LogInformation", restoreBody, StringComparison.Ordinal);
        // Der Ergebnis-Eintrag läuft nur bei tatsächlich umgeschaltetem
        // Presenter — ein verweigerter oder fehlgeschlagener Wechsel
        // (Fenster nicht greifbar, Ausnahme) meldet den unveränderten
        // Zustand nicht als Umschaltung, sondern als Warnung.
        Assert.Contains("if (SetCompactOverlay(", toggleBody, StringComparison.Ordinal);
        Assert.Contains("isActive && SetCompactOverlay(false)", restoreBody, StringComparison.Ordinal);
        Assert.Contains("private bool SetCompactOverlay", pip, StringComparison.Ordinal);

        var handler = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");
        var handlerToggleBody = SliceMethod(handler, "private async Task TogglePictureInPictureAsync()");
        // Der Ergebnis-Eintrag obliegt dem PiP-Dienst — der Handler
        // behält nur den Fehlerpfad (eine Schicht pro Ereignis).
        Assert.Contains("LogError", handlerToggleBody, StringComparison.Ordinal);
        Assert.DoesNotContain("LogInformation", handlerToggleBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Wiedergabeseite protokolliert den Wiedergabe-Fehlschlag
    /// (<c>OnMediaFailed</c>/<c>FailPlaybackStart</c> mit
    /// <c>logMessage</c>) — ohne den Inhalt der Quell-Adresse
    /// (<c>access_token</c>-Query).
    /// </summary>
    [Fact]
    public void PlayerPage_LogsPlaybackFailure()
    {
        var page = ReadFile("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

        Assert.Contains("ILogger<PlayerPage>", page, StringComparison.Ordinal);
        Assert.Contains("FailPlaybackStart(string logMessage)", page, StringComparison.Ordinal);
        var failBody = SliceMethod(page, "private void FailPlaybackStart(string logMessage)");
        Assert.Contains("LogError", failBody, StringComparison.Ordinal);
        Assert.Contains("logMessage", failBody, StringComparison.Ordinal);
        var failedBody = SliceMethod(page, "private void OnMediaFailed(object? sender, MediaFailedEventArgs e)");
        Assert.Contains("LogError", failedBody, StringComparison.Ordinal);
        // Die plattformgenerierte Fehlermeldung läuft durch die
        // Geheimnis-Bereinigung, nie roh ins Protokoll.
        Assert.Contains("SanitizeForLog(e.ErrorMessage)", failedBody, StringComparison.Ordinal);

        // Keine Logger-Zeile darf die Quell-Adresse ausgeben.
        foreach (var line in page.Split('\n').Where(l => l.Contains("logger.Log")))
        {
            Assert.DoesNotContain("ocation", line, StringComparison.Ordinal);
        }

        // Kein FailPlaybackStart-Aufruf darf die Quell-Adresse oder eine
        // rohe Ausnahmemeldung interpolieren (beide können die
        // access_token-Query tragen) — ex.Message nur bereinigt.
        foreach (var line in page.Split('\n').Where(l => l.Contains("FailPlaybackStart($", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("{location}", line, StringComparison.Ordinal);
            Assert.DoesNotContain("{ex.Message}", line, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Der Abmelde-Eintrag obliegt allein <c>AuthSessionService</c>
    /// (<c>LogoutAsync_WritesEntry</c>-Nachweis) — der Effekt-Handler
    /// hält im <c>LogoutAsync</c>-Pfad nur Aufruf und Fehlerpfad,
    /// keinen zweiten Eintrag (eine Schicht pro Ereignis).
    /// </summary>
    [Fact]
    public void ShellAppEffectHandler_LogsLogoutBreadcrumb()
    {
        var handler = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        var body = SliceMethod(handler, "private async Task LogoutAsync()");
        Assert.Contains("authSession.LogoutAsync", body, StringComparison.Ordinal);
        Assert.Contains("LogError", body, StringComparison.Ordinal);
        Assert.DoesNotContain("LogInformation", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der komplette Umstellungsumfang: kein
    /// <c>System.Diagnostics.Debug.WriteLine</c> und kein
    /// <c>Debug.WriteLine</c> mehr in den Quellen der App-Projekte
    /// (<c>VideoPlayer.Mvu</c>, <c>VideoPlayer.Maui</c>,
    /// <c>VideoPlayer.Storage</c>). Der Altbestand
    /// <c>VideoWebPlayer.Maui</c> und die Discovery-Stellen in
    /// <c>VideoPlayer.ApiClient</c> bleiben ausgenommen.
    /// </summary>
    [Fact]
    public void NoDebugWriteLine_RemainsInAppSources()
    {
        foreach (var project in new[] { "VideoPlayer.Mvu", "VideoPlayer.Maui", "VideoPlayer.Storage" })
        {
            var files = Directory.EnumerateFiles(
                    Path.Combine(Root, project), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                Assert.False(
                    source.Contains("System.Diagnostics.Debug.WriteLine", StringComparison.Ordinal)
                    || source.Contains("Debug.WriteLine", StringComparison.Ordinal),
                    $"Debug.WriteLine-Restbestand in {Path.GetRelativePath(Root, file)}");
            }
        }
    }

    /// <summary>
    /// Teil-Synchronisationen folgen Server-Ereignissen, die im Takt
    /// eingehender Fortschrittsmeldungen feuern können
    /// (<c>ContinueWatchingUpdated</c>) — der Abschluss-Eintrag bleibt
    /// deshalb unterhalb Information (wie die Hub-Ereignis-Einträge in
    /// <c>SignalRServerEventsConnection</c>); der <c>FileLogger</c>
    /// verwirft ihn sonst nicht.
    /// </summary>
    [Fact]
    public void ShellAppEffectHandler_PartialSync_StaysBelowInformation()
    {
        var handler = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        var body = SliceMethod(handler, "private async Task RunPartialSyncAsync");
        Assert.DoesNotContain("LogInformation", body, StringComparison.Ordinal);
        Assert.Contains("LogDebug", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die access_token-Maskierung existiert genau einmal
    /// (<c>AppLogService.MaskAccessToken</c>); die Wiedergabeseite
    /// wiederverwendet die gemeinsame Implementierung statt einer
    /// zweiten Regex-Kopie — die Muster können so nicht auseinanderlaufen.
    /// </summary>
    [Fact]
    public void AccessTokenMasking_SharedBetweenPageAndSink()
    {
        var log = ReadFile("VideoPlayer.Storage", "AppLogService.cs");
        Assert.Contains("public static string MaskAccessToken", log, StringComparison.Ordinal);

        var page = ReadFile("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");
        Assert.Contains("MaskAccessToken", page, StringComparison.Ordinal);
        Assert.DoesNotContain("(access_token=)", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Anmeldeergebnis-Eintrag ist als gekapselter Block
    /// (<c>LogLoginOutcome</c>) ausgeführt — nicht vierfach in den
    /// Anmeldepfaden dupliziert.
    /// </summary>
    [Fact]
    public void AuthSessionService_LoginOutcome_SingleSharedBlock()
    {
        var source = ReadFile("VideoPlayer.Mvu", "AuthSessionService.cs");

        Assert.Equal(1, CountOccurrences(source, "Anmeldung an {ServerHost}: {Status}."));
        Assert.Contains("void LogLoginOutcome(", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Ergebnis-Eintrag der Weiterschauen-Mutation ist gekapselt
    /// (<c>LogMutationOutcome</c>) — <c>HideAsync</c> und
    /// <c>SkipAsync</c> teilen denselben Meldungsblock, nur das Verb
    /// ist Parameter.
    /// </summary>
    [Fact]
    public void ContinueWatchingMutationService_MutationOutcome_SingleSharedBlock()
    {
        var source = ReadFile("VideoPlayer.Mvu", "ContinueWatchingMutationService.cs");

        // Erfolgs- und Misserfolg-Template existieren je genau einmal.
        Assert.Equal(2, CountOccurrences(source, "Weiterschauen-Eintrag {MediaType}/{MediaId}"));
        Assert.Contains("void LogMutationOutcome(", source, StringComparison.Ordinal);
    }

    /// <summary>Liefert den Rumpf der Methode ab ihrer Signatur bis zum nächsten Member (Sichtbarkeit, Dokumentationskommentar, Attribut oder Klassenende — nicht nur <c>private</c>).</summary>
    /// <param name="source">Der zu durchsuchende Quelltext.</param>
    /// <param name="signature">Die Methodensignatur, ab der geschnitten wird.</param>
    /// <returns>Der ausgeschnittene Methodenrumpf.</returns>
    private static string SliceMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Methode nicht gefunden: {signature}");
        var searchFrom = start + signature.Length;
        var next = new[]
            {
                "\n    /// <", "\n    private ", "\n    public ",
                "\n    protected ", "\n    internal ", "\n    [",
                "\n    #if", "\n    #endif", "\n    }",
            }
            .Select(marker => source.IndexOf(marker, searchFrom, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .DefaultIfEmpty(-1)
            .Min();
        return next < 0 ? source[start..] : source[start..next];
    }

    /// <summary>Zählt die Vorkommen eines Literals im Quelltext.</summary>
    /// <param name="source">Der zu durchsuchende Quelltext.</param>
    /// <param name="token">Das gesuchte Literal.</param>
    /// <returns>Anzahl der nicht überlappenden Vorkommen.</returns>
    private static int CountOccurrences(string source, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
