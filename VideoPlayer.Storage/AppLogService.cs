using Microsoft.Extensions.Logging;

namespace VideoPlayer.Storage;

/// <summary>
/// Schaltbarer Datei-Speicher des Programmprotokolls: schreibt
/// zeitgestempelte Protokollzeilen in <c>videoplayer.log</c> unterhalb von
/// <see cref="AppStorageLayout.LogsDirectory"/> (nicht backup-pflichtig,
/// profil-isoliert). Der Schaltzustand folgt der Einstellung
/// <see cref="AppSettings.LoggingEnabled"/> — der Startwert wird beim
/// Aufbau synchron übergeben, Laufzeit-Umschaltungen laufen über
/// <see cref="SetEnabled"/>. Überschreitet die aktuelle Datei
/// <see cref="MaxLogFileBytes"/>, wird sie nach <c>videoplayer.previous.log</c>
/// rotiert. Sämtliche Schreibfehler werden geschluckt — die
/// Protokollierung darf die App niemals werfen lassen.
/// </summary>
public sealed class AppLogService
{
    /// <summary>Maximale Größe der aktuellen Protokolldatei in Bytes (1 MiB), danach Rotation.</summary>
    public const long MaxLogFileBytes = 1_048_576;

    /// <summary>Dateiname der aktuellen Protokolldatei.</summary>
    public const string LogFileName = "videoplayer.log";

    /// <summary>Dateiname der rotierten Vorgänger-Protokolldatei.</summary>
    public const string PreviousLogFileName = "videoplayer.previous.log";

    private readonly object gate = new();
    private readonly string logsDirectory;
    private bool enabled;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="layout">Ablage-Layout (liefert <see cref="AppStorageLayout.LogsDirectory"/>).</param>
    /// <param name="enabled">Schaltzustand beim Start (aus der persistenten Einstellung).</param>
    public AppLogService(AppStorageLayout layout, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(layout);
        this.logsDirectory = layout.LogsDirectory;
        this.enabled = enabled;
        if (enabled)
        {
            WriteMarker("=== Protokollierung beim Start aktiv ===");
        }
    }

    /// <summary><c>true</c>, solange die Protokollierung aktiv ist.</summary>
    public bool IsEnabled
    {
        get { lock (gate) { return enabled; } }
    }

    /// <summary>Vollständiger Pfad der aktuellen Protokolldatei.</summary>
    public string LogFilePath
    {
        get { return Path.Combine(logsDirectory, LogFileName); }
    }

    /// <summary>Vollständiger Pfad der rotierten Vorgänger-Protokolldatei.</summary>
    public string PreviousLogFilePath
    {
        get { return Path.Combine(logsDirectory, PreviousLogFileName); }
    }

    /// <summary>
    /// Schaltet die Protokollierung um; beide Richtungen schreiben eine
    /// Marker-Zeile, sofern die Datei erreichbar ist.
    /// </summary>
    /// <param name="value">Neuer Schaltzustand.</param>
    public void SetEnabled(bool value)
    {
        lock (gate)
        {
            if (value == enabled)
            {
                return;
            }

            if (value)
            {
                enabled = true;
                WriteMarker("=== Protokollierung aktiviert ===");
            }
            else
            {
                WriteMarker("=== Protokollierung deaktiviert ===");
                enabled = false;
            }
        }
    }

    /// <summary>
    /// Hängt eine Protokollzeile an (nur bei aktivierter Protokollierung);
    /// bei Bedarf rotiert die Datei vorher auf die Vorgänger-Datei.
    /// </summary>
    /// <param name="level">Log-Stufe des Eintrags.</param>
    /// <param name="category">Kategorie (Logger-Name bzw. Herkunft).</param>
    /// <param name="message">Protokolltext.</param>
    /// <param name="exception">Optionale Ausnahme — wird vollständig angehängt.</param>
    public void Write(LogLevel level, string category, string message, Exception? exception = null)
    {
        lock (gate)
        {
            if (!enabled)
            {
                return;
            }

            try
            {
                RotateIfNeeded();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                // Zentrale Geheimnis-Maskierung am Sinkenpunkt: Meldungen
                // und angehängte Ausnahmen können eine Quell-URL mit
                // access_token-Query enthalten — der Token-Wert darf
                // nicht in die versendbare Protokolldatei gelangen.
                var line = $"{timestamp} [{LevelName(level)}] {category}: {MaskAccessToken(message)}";
                if (exception is not null)
                {
                    line += Environment.NewLine + MaskAccessToken(exception.ToString());
                }

                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch (Exception)
            {
                // Die Protokollierung darf die App niemals werfen lassen.
            }
        }
    }

    /// <summary>
    /// Liefert die vorhandenen Protokolldateien für den Versand als
    /// Mail-Anhang (aktuelle zuerst, dann die rotierte Vorgänger-Datei).
    /// </summary>
    /// <returns>Pfade der vorhandenen, nicht leeren Protokolldateien.</returns>
    public IReadOnlyList<string> GetAttachmentPaths()
    {
        lock (gate)
        {
            var paths = new List<string>();
            foreach (var path in new[] { LogFilePath, PreviousLogFilePath })
            {
                try
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 0)
                    {
                        paths.Add(path);
                    }
                }
                catch (Exception)
                {
                    // Zugriffsfehler beim Auflisten sind unkritisch.
                }
            }

            return paths;
        }
    }

    private void WriteMarker(string marker)
    {
        try
        {
            Directory.CreateDirectory(logsDirectory);
            File.AppendAllText(LogFilePath, marker + Environment.NewLine);
        }
        catch (Exception)
        {
            // Die Protokollierung darf die App niemals werfen lassen.
        }
    }

    private void RotateIfNeeded()
    {
        if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileBytes)
        {
            if (File.Exists(PreviousLogFilePath))
            {
                File.Delete(PreviousLogFilePath);
            }

            File.Move(LogFilePath, PreviousLogFilePath);
        }
    }

    /// <summary>
    /// Maskiert den Wert eines <c>access_token</c>-Query-Parameters in
    /// einem Text — die gemeinsame Geheimnis-Bereinigung aller
    /// Protokoll-Schreiber (die Sinken-Maskierung in <see cref="Write"/>
    /// bleibt als zweite Verteidigungslinie bestehen).
    /// </summary>
    /// <param name="text">Der zu bereinigende Text.</param>
    /// <returns>Der Text mit maskierten <c>access_token</c>-Werten.</returns>
    public static string MaskAccessToken(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            text, @"(access_token=)[^&\s""'<>]*", "$1***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRIT",
        _ => "INFO"
    };
}
