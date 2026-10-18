using Microsoft.Extensions.Logging;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Ein von <see cref="RecordingLoggerProvider"/> aufgezeichneter
/// Protokolleintrag.
/// </summary>
/// <param name="Level">Stufe des Eintrags.</param>
/// <param name="Category">Logger-Kategorie (Typname).</param>
/// <param name="Message">Formatierte Meldung.</param>
/// <param name="Exception">Zugehörige Ausnahme, falls vorhanden.</param>
/// <returns>Ein neuer aufgezeichneter Protokolleintrag.</returns>
internal sealed record RecordedLogEntry(
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception);
