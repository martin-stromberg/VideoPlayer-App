using Microsoft.Maui.Storage;
using Zeroconf;
using System.Net;
using System.Net.Sockets;

namespace VideoWebPlayer.Maui.Services;

public class SettingsService : ISettingsService
{
    private const string ServerKey = "ServerAddress";
    private const string AuthTokenKey = "AuthToken";
    private const string UserKey = "Username";
    private const string PasswordKey = "Passkey";

    private const string PlaybackCacheDaysKey = "PlaybackCacheRetentionDays";
    private const string WatchlistCacheDaysKey = "WatchlistCacheRetentionDays";
    private const string DownloadRetentionDaysKey = "DownloadRetentionDays";

    private string GetPrefixedKey(string key)
    {
        var prefix = ProfileManager.Instance.GetPreferencesPrefix();
        return prefix + key;
    }

    private static string ReadSecureValue(string key)
    {
        try
        {
            return SecureStorage.Default.GetAsync(key).GetAwaiter().GetResult() ?? string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not read secure storage key '{key}': {ex.Message}");
            return string.Empty;
        }
    }

    private static void WriteSecureValue(string key, string? value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                SecureStorage.Default.Remove(key);
                return;
            }

            SecureStorage.Default.SetAsync(key, value).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not write secure storage key '{key}': {ex.Message}");
        }
    }

    private string GetMigratedSecureValue(string key)
    {
        var prefixedKey = GetPrefixedKey(key);
        var secureValue = ReadSecureValue(prefixedKey);
        if (!string.IsNullOrEmpty(secureValue))
            return secureValue;

        var legacyValue = Preferences.Default.Get(prefixedKey, string.Empty);
        if (string.IsNullOrEmpty(legacyValue))
            return string.Empty;

        WriteSecureValue(prefixedKey, legacyValue);
        Preferences.Default.Remove(prefixedKey);
        return legacyValue;
    }

    public string? ServerAddress => Preferences.Default.Get(GetPrefixedKey(ServerKey), string.Empty);

    public int PlaybackCacheRetentionDays
    {
        get => ClampDays(Preferences.Default.Get(GetPrefixedKey(PlaybackCacheDaysKey), 1));
        set => Preferences.Default.Set(GetPrefixedKey(PlaybackCacheDaysKey), ClampDays(value));
    }

    public int WatchlistCacheRetentionDays
    {
        get => ClampDays(Preferences.Default.Get(GetPrefixedKey(WatchlistCacheDaysKey), 3));
        set => Preferences.Default.Set(GetPrefixedKey(WatchlistCacheDaysKey), ClampDays(value));
    }

    public int DownloadRetentionDays
    {
        get => ClampDays(Preferences.Default.Get(GetPrefixedKey(DownloadRetentionDaysKey), 7));
        set => Preferences.Default.Set(GetPrefixedKey(DownloadRetentionDaysKey), ClampDays(value));
    }

    private static int ClampDays(int days)
    {
        if (days < 1) return 1;
        if (days > 365) return 365;
        return days;
    }

    public bool HasServerAddress()
    {
        var value = Preferences.Default.Get(GetPrefixedKey(ServerKey), string.Empty);
        return !string.IsNullOrWhiteSpace(value);
    }

    public void SetServerAddress(string address)
    {
        Preferences.Default.Set(GetPrefixedKey(ServerKey), address ?? string.Empty);
    }

    public void ClearServerAddress()
    {
        Preferences.Default.Remove(GetPrefixedKey(ServerKey));
    }

    public string GetUsername()
    {
        return Preferences.Default.Get(GetPrefixedKey(UserKey), string.Empty);
    }

    public string GetPassword()
    {
        return GetMigratedSecureValue(PasswordKey);
    }

    public bool HasCredentials()
    {
        return !string.IsNullOrWhiteSpace(GetUsername()) && !string.IsNullOrWhiteSpace(GetPassword());
    }

    public void SetCredentials(string username, string password)
    {
        Preferences.Default.Set(GetPrefixedKey(UserKey), username ?? string.Empty);
        WriteSecureValue(GetPrefixedKey(PasswordKey), password);
    }

    public void ClearCredentials()
    {
        Preferences.Default.Remove(GetPrefixedKey(UserKey));
        WriteSecureValue(GetPrefixedKey(PasswordKey), null);
        ClearAuthToken();
    }

    /// <summary>
    /// Gets the authentication token for the current profile.
    /// </summary>
    public string? GetAuthToken()
    {
        return GetMigratedSecureValue(AuthTokenKey);
    }

    /// <summary>
    /// Sets the authentication token for the current profile.
    /// </summary>
    public void SetAuthToken(string token)
    {
        WriteSecureValue(GetPrefixedKey(AuthTokenKey), token);
        Preferences.Default.Remove(GetPrefixedKey(AuthTokenKey));
    }

    /// <summary>
    /// Clears the authentication token for the current profile.
    /// </summary>
    public void ClearAuthToken()
    {
        Preferences.Default.Remove(GetPrefixedKey(AuthTokenKey));
        WriteSecureValue(GetPrefixedKey(AuthTokenKey), null);
    }

    // mDNS Discovery (Zeroconf)
    public async Task<IReadOnlyList<string>> DiscoverServersAsync(int timeoutMs = 3000)
    {
        var found = new List<string>();
        try
        {
            var responses = await ZeroconfResolver.ResolveAsync("_http._tcp.local.", TimeSpan.FromMilliseconds(timeoutMs));
            foreach (var resp in responses)
            {
                foreach (var svc in resp.Services.Values)
                {
                    var ip = resp.IPAddress;
                    var port = svc.Port;
                    found.Add($"http://{ip}:{port}");
                }
            }
        }
        catch { }
        return found;
    }

    // UDP Broadcast Discovery (Fallback)
    public async Task<IReadOnlyList<string>> DiscoverServersUdpAsync(int port = 5001, int timeoutMs = 2000)
    {
        var found = new List<string>();
        using var client = new UdpClient();
        client.EnableBroadcast = true;
        var request = System.Text.Encoding.UTF8.GetBytes("VIDEOWEBPLAYER_DISCOVERY");
        var broadcastAddr = new IPEndPoint(IPAddress.Broadcast, port);
        await client.SendAsync(request, request.Length, broadcastAddr);

        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (client.Available > 0)
            {
                var result = await client.ReceiveAsync();
                var response = System.Text.Encoding.UTF8.GetString(result.Buffer);
                if (response.StartsWith("VIDEOWEBPLAYER_SERVER:"))
                {
                    found.Add(response.Substring("VIDEOWEBPLAYER_SERVER:".Length));
                }
            }
            await Task.Delay(100);
        }
        return found;
    }
}
