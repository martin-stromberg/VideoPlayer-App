using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>In-Memory-Fake der sicheren Ablage für Tests.</summary>
internal sealed class InMemorySecretStore : ISecretStore
{
    private readonly Dictionary<string, string> values = new();

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(values.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value)
    {
        values[key] = value;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key) => Task.FromResult(values.Remove(key));

    /// <summary>Alle belegten Schlüssel (für Isolationsprüfungen).</summary>
    public IReadOnlyCollection<string> Keys => values.Keys;
}
