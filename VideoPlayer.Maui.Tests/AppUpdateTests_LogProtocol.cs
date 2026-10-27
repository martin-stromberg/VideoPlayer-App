using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>AppUpdate.Update</c> — Bereich Protokollversand.</summary>
public class AppUpdateTests_LogProtocol
{
    /// <summary>Prüft Update: ohne aktivierte Protokollierung bleibt der Versand wirkungslos.</summary>
    [Fact]
    public void Update_SendLogRequested_WhenDisabled_IsUnchanged()
    {
        var model = new AppModel { LogSendError = "alt" };

        var transition = AppUpdate.Update(model, new SendLogProtocolRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei aktivierter Protokollierung löst die Anforderung den Versand-Effekt aus und räumt den Fehlerhinweis.</summary>
    [Fact]
    public void Update_SendLogRequested_WhenEnabled_EmitsEffectAndClearsError()
    {
        var model = new AppModel
        {
            Settings = new AppSettings { LoggingEnabled = true },
            LogSendError = "alter Fehler"
        };

        var transition = AppUpdate.Update(model, new SendLogProtocolRequestedMessage());

        Assert.Null(transition.Model.LogSendError);
        var effect = Assert.Single(transition.Effects);
        Assert.IsType<SendLogProtocolEffect>(effect);
    }

    /// <summary>Prüft Update: der Versand-Abschluss setzt den Fehlertext in das Modell.</summary>
    [Fact]
    public void Update_SendLogCompleted_WithError_SetsLogSendError()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new SendLogProtocolCompletedMessage(AppUpdate.LogSendFailedError));

        Assert.Equal(AppUpdate.LogSendFailedError, transition.Model.LogSendError);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: ein erfolgreicher Versand räumt einen vorhandenen Fehlertext.</summary>
    [Fact]
    public void Update_SendLogCompleted_WithoutError_ClearsLogSendError()
    {
        var transition = AppUpdate.Update(
            new AppModel { LogSendError = "alter Fehler" },
            new SendLogProtocolCompletedMessage(null));

        Assert.Null(transition.Model.LogSendError);
    }
}
