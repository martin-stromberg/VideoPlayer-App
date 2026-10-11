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
}
