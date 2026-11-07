#if DEBUG
using System.Globalization;

namespace VideoPlayer.Maui.Services;

/// <summary>Explizit aktivierte Dateiuhr für isolierte Debug-E2E-Profile.</summary>
internal sealed class SeasonalTestTimeProvider : TimeProvider
{
    private readonly string path;

    private SeasonalTestTimeProvider(string path) => this.path = path;

    internal static TimeProvider Create()
    {
        var path = Environment.GetEnvironmentVariable("VIDEOPLAYER_TEST_CLOCK_FILE");
        var profile = Environment.GetEnvironmentVariable("VIDEOPLAYER_PROFILE");
        return !string.IsNullOrWhiteSpace(path) && profile?.StartsWith("e2e-", StringComparison.Ordinal) == true
            ? new SeasonalTestTimeProvider(path) : System;
    }

    public override DateTimeOffset GetUtcNow()
    {
        try
        {
            if (DateTimeOffset.TryParse(File.ReadAllText(path), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value))
                return value.ToUniversalTime();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return System.GetUtcNow();
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        System.CreateTimer(callback, state, dueTime > TimeSpan.FromMilliseconds(250) ? TimeSpan.FromMilliseconds(250) : dueTime, period);
}
#endif
