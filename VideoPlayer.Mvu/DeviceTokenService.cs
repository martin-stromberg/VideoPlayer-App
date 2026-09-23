using System.Diagnostics;
using System.Net;
using System.Text;
using VideoPlayer.ApiClient;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Verwaltet die gepaarten Geräte-Tokens: lädt sie serveradress-spezifisch aus
/// der sicheren Ablage, hält sie über <see cref="ClientGate.DeviceTokenProvider"/>
/// für den API-Client bereit und meldet serverseitige Ablehnungen des Tokens
/// über <see cref="DeviceTokenRejected"/>. Fällt ohne Geräte-Token auf die
/// statische Auflösung (<see cref="ClientGate.ResolveStatic"/>) zurück.
/// </summary>
public sealed class DeviceTokenService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly AppSecretsService secrets;
    private readonly Func<Uri, HttpClient> httpClientFactory;

    // Serialisiert die Provider-Mutationen: Prüfung + Aktivierung des
    // Pairing-Ergebnisses (SetPairedTokenIfCurrentAsync) darf sich nicht
    // mit einem zwischenzeitlichen Serverwechsel (LoadForServerAsync)
    // überlagern — sonst ist der Stale-Guard ein Check-then-act.
    private readonly SemaphoreSlim providerLock = new(1, 1);

    /// <summary>Erstellt den Dienst mit einem Standard-<see cref="HttpClient"/> für Gate-Probes.</summary>
    /// <param name="secrets">Sichere Ablage der Geräte-Tokens.</param>
    public DeviceTokenService(AppSecretsService secrets)
        : this(secrets, null)
    {
    }

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="secrets">Sichere Ablage der Geräte-Tokens.</param>
    /// <param name="httpClientFactory">
    /// Fabrik für den HTTP-Client der Gate-Probe (injizierbar für Tests);
    /// <c>null</c> = Standard-Client mit 10-s-Timeout.
    /// </param>
    public DeviceTokenService(AppSecretsService secrets, Func<Uri, HttpClient>? httpClientFactory)
    {
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.httpClientFactory = httpClientFactory ?? CreateHttpClient;
    }

    /// <summary>Die Serveradresse, deren Geräte-Token der Provider aktuell bedient.</summary>
    public string? CurrentServerAddress { get; private set; }

    /// <summary>
    /// Wird ausgelöst, wenn der Server das bereitgestellte Geräte-Token
    /// verwirft (Gate-Ablehnung). Der Abonnent dispatcht daraufhin die
    /// Re-Pairing-Nachricht.
    /// </summary>
    public event EventHandler? DeviceTokenRejected;

    /// <summary>
    /// Lädt das Geräte-Token des übergebenen Servers aus der sicheren Ablage
    /// und stellt den <see cref="ClientGate.DeviceTokenProvider"/> darauf ein
    /// (ohne Token: statische Auflösung).
    /// </summary>
    /// <param name="serverAddress">Aktive Serveradresse oder <c>null</c>.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task LoadForServerAsync(string? serverAddress, CancellationToken cancellationToken = default)
    {
        string? token = null;
        if (!string.IsNullOrWhiteSpace(serverAddress))
        {
            try
            {
                token = await secrets.GetDeviceTokenAsync(serverAddress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTokenService] Geräte-Token konnte nicht geladen werden: {ex.Message}");
            }
        }

        await providerLock.WaitAsync();
        try
        {
            CurrentServerAddress = serverAddress;
            ApplyProvider(token);
        }
        finally
        {
            providerLock.Release();
        }
    }

    /// <summary>
    /// Persistiert ein frisch gepaartes Geräte-Token und aktiviert es im
    /// <see cref="ClientGate.DeviceTokenProvider"/> nur, wenn
    /// <paramref name="serverAddress"/> noch der aktuell bediente Server
    /// ist — atomar unter <see cref="providerLock"/>, damit ein
    /// zwischenzeitlicher Serverwechsel nicht vom verspäteten
    /// Pairing-Ergebnis überschrieben wird (atomarer Stale-Guard des
    /// Effekt-Handlers). Bei veraltetem Kontext bleibt das Token nur
    /// unter seiner eigenen Adresse hinterlegt.
    /// </summary>
    /// <param name="serverAddress">Serveradresse des Pairings.</param>
    /// <param name="token">Das entschlüsselte Geräte-Token.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>
    /// <c>true</c>, wenn das Token im Provider aktiviert wurde;
    /// <c>false</c>, wenn der Pairing-Kontext veraltet ist (Token nur
    /// hinterlegt).
    /// </returns>
    public async Task<bool> SetPairedTokenIfCurrentAsync(
        string serverAddress,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        await providerLock.WaitAsync();
        try
        {
            await secrets.SetDeviceTokenAsync(serverAddress, token);
            if (CurrentServerAddress is null || !SameServer(CurrentServerAddress, serverAddress))
            {
                return false;
            }

            ApplyProvider(token);
            return true;
        }
        finally
        {
            providerLock.Release();
        }
    }

    /// <summary>
    /// Verwirft das aktuell bediente Geräte-Token: löscht es aus der sicheren
    /// Ablage und setzt den Provider auf die statische Auflösung zurück.
    /// </summary>
    public async Task DiscardCurrentAsync()
    {
        string? current;
        await providerLock.WaitAsync();
        try
        {
            current = CurrentServerAddress;
            CurrentServerAddress = null;
            ApplyProvider(null);
        }
        finally
        {
            providerLock.Release();
        }

        if (current is not null)
        {
            try
            {
                await secrets.ClearDeviceTokenAsync(current);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTokenService] Geräte-Token konnte nicht verworfen werden: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Verwirft das Geräte-Token nur, wenn <paramref name="serverAddress"/> der
    /// aktuell bediente Server ist (z. B. Gate-Ablehnung eines Kandidaten).
    /// Vergleich und Provider-Rücksetzung laufen atomar unter
    /// <see cref="providerLock"/> — ein dazwischengeschobener Serverwechsel
    /// (<see cref="LoadForServerAsync"/>) kann so nicht das frisch
    /// aktivierte Token des neuen Servers verwerfen.
    /// </summary>
    /// <param name="serverAddress">Die abgelehnte Serveradresse.</param>
    /// <returns>
    /// <c>true</c>, wenn die Adresse der aktuell bediente Server war und
    /// verworfen wurde; <c>false</c> bei einem anderen aktiven Server.
    /// </returns>
    public async Task<bool> DiscardIfCurrentAsync(string serverAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);

        string? discarded;
        await providerLock.WaitAsync();
        try
        {
            if (CurrentServerAddress is null || !SameServer(CurrentServerAddress, serverAddress))
            {
                return false;
            }

            discarded = CurrentServerAddress;
            CurrentServerAddress = null;
            ApplyProvider(null);
        }
        finally
        {
            providerLock.Release();
        }

        // Store-Löschung erst nach dem Release — die Secure-Storage-I/O
        // soll die Provider-Serialisierung nicht aufhalten.
        try
        {
            await secrets.ClearDeviceTokenAsync(discarded);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeviceTokenService] Geräte-Token konnte nicht verworfen werden: {ex.Message}");
        }

        return true;
    }

    /// <summary>
    /// Prüft per <c>POST api/auth/login</c> (einziger MauiOnly-gated
    /// Endpunkt — der Health-Endpunkt liegt serverseitig nicht hinter dem
    /// Client-Gate und kann eine Ablehnung nicht erkennen) mit leerem
    /// JSON-Body und nur dem Gate-Header (ohne Bearer), ob das
    /// Client-Gate den Schlüssel ablehnt. Der Gate-Schlüssel wird
    /// adressspezifisch aufgelöst (<see cref="ResolveGateKeyAsync"/>) —
    /// niemals der ambiente Provider, der ggf. das Geräte-Token eines
    /// anderen (aktiven) Servers an diese Adresse senden würde. Die Probe
    /// erzeugt keine IP-Sperr-Fehlversuche (die Fehlerzählung läuft
    /// ausschließlich im Pairing-Endpunkt).
    /// </summary>
    /// <param name="serverAddress">Zu prüfende Serveradresse.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>
    /// <c>true</c> bei HTTP 401 mit <c>application/problem+json</c>
    /// (Gate-Ablehnung aus dem Action-Filter); <c>false</c> bei 401 mit
    /// <c>text/plain</c> (Credential-Fehler — Schlüssel akzeptiert), 400
    /// (Model-Validation), 429, sonstigen Antworten und bei Fehlern.
    /// </returns>
    public async Task<bool> IsGateRejectedAsync(string serverAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var gateKey = await ResolveGateKeyAsync(serverAddress);
            using var http = httpClientFactory(new Uri(serverAddress));
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(ClientGate.HeaderName, gateKey);
            using var response = await http.SendAsync(request, cancellationToken);
            return response.StatusCode == HttpStatusCode.Unauthorized
                && string.Equals(
                    response.Content?.Headers.ContentType?.MediaType,
                    "application/problem+json",
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ein angeforderter Abbruch ist keine „nicht abgelehnt"-
            // Antwort — er wird durchgereicht, damit Aufrufer nicht
            // fälschlich den SessionExpired-/Widerrufs-Pfad beschreiten.
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            Debug.WriteLine($"[DeviceTokenService] Gate-Probe fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    /// <summary>Löst <see cref="DeviceTokenRejected"/> aus.</summary>
    public void NotifyRejected()
    {
        try
        {
            DeviceTokenRejected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeviceTokenService] DeviceTokenRejected-Handler fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Löst den Gate-Schlüssel für eine konkrete Serveradresse auf:
    /// hinterlegtes Geräte-Token der Adresse, sonst die statischen Quellen
    /// (Muster: <c>ServerHealthProbe.ResolveGateKeyAsync</c>). Der ambiente
    /// <see cref="ClientGate.DeviceTokenProvider"/> bedient ggf. einen
    /// anderen Server — dessen Token an eine fremde Adresse zu senden wäre
    /// eine Credential-Weitergabe und würde die falsche Identität prüfen.
    /// </summary>
    /// <param name="serverAddress">Die zu prüfende Serveradresse.</param>
    /// <returns>Der aufgelöste Gate-Schlüssel bzw. der Platzhalter.</returns>
    private async Task<string> ResolveGateKeyAsync(string serverAddress)
    {
        try
        {
            var deviceToken = await secrets.GetDeviceTokenAsync(serverAddress);
            if (!string.IsNullOrWhiteSpace(deviceToken))
            {
                return deviceToken;
            }
        }
        catch (Exception ex)
        {
            // Ein Lesefehler der sicheren Ablage blockiert die Probe
            // nicht — die statische Auflösung greift.
            Debug.WriteLine($"[DeviceTokenService] Geräte-Token der Probe-Adresse nicht lesbar: {ex.Message}");
        }

        return ClientGate.ResolveStatic();
    }

    private static HttpClient CreateHttpClient(Uri baseAddress) =>
        new() { BaseAddress = baseAddress, Timeout = ProbeTimeout };

    private void ApplyProvider(string? deviceToken)
    {
        ClientGate.DeviceTokenProvider = string.IsNullOrWhiteSpace(deviceToken)
            ? ClientGate.ResolveStatic
            : () => deviceToken;
    }

    private static bool SameServer(string left, string right)
    {
        var normalizedLeft = ServerAddress.TryNormalize(left, out var l) ? l : left.Trim();
        var normalizedRight = ServerAddress.TryNormalize(right, out var r) ? r : right.Trim();
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }
}
