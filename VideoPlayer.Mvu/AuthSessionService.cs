using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Orchestriert die Anmeldung am VideoPlayer-Server (Service Layer):
/// manuelle und automatische Anmeldung über <see cref="VideoPlayerApiClient.LoginAsync"/>,
/// Persistenz von Zugangsdaten und Anmelde-Token über <see cref="AppSecretsService"/>
/// (sichere Ablage — Geheimnisse gelangen nicht ins Cloud-Backup) sowie die
/// stille Neuanmeldung als <see cref="VideoPlayerApiClientOptions.TokenRefresher"/>
/// des gehaltenen API-Clients.
/// </summary>
public sealed class AuthSessionService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly AppSecretsService secrets;
    private readonly Func<Uri, HttpClient> httpClientFactory;
    private readonly Func<TimeSpan, CancellationToken, Task>? delayAsync;

    /// <summary>Erstellt den Dienst mit der produktiven <see cref="HttpClient"/>-Erzeugung.</summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    public AuthSessionService(AppSecretsService secrets)
        : this(secrets, null, null)
    {
    }

    /// <summary>
    /// Erstellt den Dienst mit einer injizierbaren <see cref="HttpClient"/>-Fabrik
    /// und optionaler Warte-Implementierung für die Wiederholungsstrategie
    /// (für Tests; produktiv wird ein Client mit Basisadresse und Timeout erzeugt).
    /// </summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    /// <param name="httpClientFactory">Fabrik für <see cref="HttpClient"/> pro Serveradresse; <c>null</c> = produktive Erzeugung mit Timeout.</param>
    /// <param name="delayAsync">Optionale Warte-Implementierung der Wiederholungsstrategie (Tests); <c>null</c> = <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    public AuthSessionService(
        AppSecretsService secrets,
        Func<Uri, HttpClient>? httpClientFactory,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.httpClientFactory = httpClientFactory ?? CreateHttpClient;
        this.delayAsync = delayAsync;
    }

    /// <summary>
    /// Zuletzt erfolgreich angemeldeter API-Client (Bearer-Token gesetzt,
    /// stille Neuanmeldung bei 401 verdrahtet); <c>null</c> vor der ersten
    /// erfolgreichen Anmeldung.
    /// </summary>
    public VideoPlayerApiClient? Client { get; private set; }

    /// <summary>Basisadresse des Servers, an dem <see cref="Client"/> angemeldet ist.</summary>
    public string? ClientBaseAddress { get; private set; }

    /// <summary>
    /// Wird ausgelöst, wenn die stille Neuanmeldung endgültig scheitert
    /// (Zugangsdaten fehlen oder werden vom Server abgelehnt). Transport- oder
    /// Serverfehler lösen das Ereignis nicht aus.
    /// </summary>
    public event EventHandler? SessionExpired;

    /// <summary>
    /// Automatische Anmeldung beim Start bzw. nach einem Serverwechsel:
    /// nutzt die gespeicherten Zugangsdaten; <see cref="AuthAttemptStatus.MissingCredentials"/>,
    /// wenn keine vorhanden sind (z. B. nach einer Backup-Wiederherstellung).
    /// Der gelieferte <see cref="AuthAttempt.Username"/> ist der gespeicherte,
    /// nicht geheime Benutzername (Vorbefüllung der Anmeldemaske).
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers, an dem angemeldet wird.</param>
    /// <param name="cancellationToken">Bricht die Anmeldung ab.</param>
    /// <returns>Das Ergebnis des Anmeldeversuchs.</returns>
    public async Task<AuthAttempt> TryStoredLoginAsync(Uri serverAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverAddress);

        var username = secrets.GetUsername();
        if (!await secrets.HasCredentialsAsync())
        {
            return new AuthAttempt(AuthAttemptStatus.MissingCredentials, EmptyToNull(username));
        }

        var password = await secrets.GetPasswordAsync();
        var attempt = await LoginCoreAsync(serverAddress, username, password, cancellationToken);
        return attempt with { Username = EmptyToNull(username) };
    }

    /// <summary>
    /// Manuelle Anmeldung mit eingegebenen Zugangsdaten; bei Erfolg werden
    /// Zugangsdaten und Token gespeichert und <see cref="Client"/> gesetzt.
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers, an dem angemeldet wird.</param>
    /// <param name="username">Benutzername (wird getrimmt).</param>
    /// <param name="password">Passwort.</param>
    /// <param name="cancellationToken">Bricht die Anmeldung ab.</param>
    /// <returns>Das Ergebnis des Anmeldeversuchs.</returns>
    public async Task<AuthAttempt> LoginAsync(
        Uri serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return await LoginCoreAsync(serverAddress, username.Trim(), password, cancellationToken);
    }

    /// <summary>
    /// Beendet die Sitzung lokal („Abmelden"): der gehaltene API-Client
    /// wird freigegeben und die gespeicherten Zugangsdaten samt Token
    /// entfernt — danach ist keine automatische Anmeldung mehr möglich.
    /// Die API besitzt keinen Logout-Endpunkt (zustandslose JWT); die
    /// Sitzung endet durch Verwerfen der lokalen Geheimnisse.
    /// </summary>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Räumen der Sitzung.</returns>
    public async Task LogoutAsync()
    {
        Client = null;
        ClientBaseAddress = null;
        try
        {
            await secrets.ClearCredentialsAsync();
        }
        catch (Exception ex)
        {
            // Ein Fehler der sicheren Ablage verhindert die Abmeldung
            // nicht — der Client ist bereits freigegeben; Reste werden
            // bei der nächsten Anmeldung überschrieben.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Zugangsdaten entfernen fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task<AuthAttempt> LoginCoreAsync(
        Uri serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var client = CreateClient(serverAddress);
        try
        {
            var token = await client.LoginAsync(username, password, cancellationToken);
            await secrets.SetCredentialsAsync(username, password);
            await secrets.SetAuthTokenAsync(token.Token);
            Client = client;
            ClientBaseAddress = serverAddress.ToString();
            return new AuthAttempt(AuthAttemptStatus.Succeeded, username);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new AuthAttempt(AuthAttemptStatus.InvalidCredentials, username);
        }
        catch (ApiException ex) when (ex.StatusCode is null)
        {
            // Transportfehler/Zeitüberschreitung nach Ausschöpfung der Wiederholungsstrategie.
            return new AuthAttempt(AuthAttemptStatus.ServerUnreachable, username);
        }
        catch (ApiException)
        {
            return new AuthAttempt(AuthAttemptStatus.Failed, username);
        }
        catch (HttpRequestException)
        {
            return new AuthAttempt(AuthAttemptStatus.ServerUnreachable, username);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AuthAttempt(AuthAttemptStatus.ServerUnreachable, username);
        }
    }

    /// <summary>
    /// Erstellt einen API-Client für die Serveradresse mit verdrahteter
    /// stiller Neuanmeldung (<see cref="VideoPlayerApiClientOptions.TokenRefresher"/>).
    /// </summary>
    /// <param name="serverAddress">Basisadresse des Servers.</param>
    /// <returns>Der konfigurierte API-Client.</returns>
    private VideoPlayerApiClient CreateClient(Uri serverAddress)
    {
        var http = httpClientFactory(serverAddress);
        return new VideoPlayerApiClient(http, new VideoPlayerApiClientOptions
        {
            BaseAddress = serverAddress,
            DelayAsync = delayAsync,
            TokenRefresher = cancellationToken => SilentReloginAsync(serverAddress, cancellationToken)
        });
    }

    /// <summary>
    /// Stille Neuanmeldung mit den gespeicherten Zugangsdaten über einen
    /// eigenen kurzlebigen Client (vermeidet Rekursion im aufrufenden
    /// Request). Bei fehlenden oder abgelehnten Zugangsdaten wird
    /// <see cref="SessionExpired"/> ausgelöst und <c>null</c> geliefert;
    /// bei vorübergehenden Fehlern nur <c>null</c>.
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers, an dem neu angemeldet wird.</param>
    /// <param name="cancellationToken">Bricht die Neuanmeldung ab.</param>
    /// <returns>Das neue Anmelde-Token; <c>null</c> bei Fehlschlag.</returns>
    private async Task<AuthToken?> SilentReloginAsync(Uri serverAddress, CancellationToken cancellationToken)
    {
        if (!await secrets.HasCredentialsAsync())
        {
            await secrets.ClearAuthTokenAsync();
            OnSessionExpired();
            return null;
        }

        var username = secrets.GetUsername();
        var password = await secrets.GetPasswordAsync();
        try
        {
            using var http = httpClientFactory(serverAddress);
            var refreshClient = new VideoPlayerApiClient(http, new VideoPlayerApiClientOptions
            {
                BaseAddress = serverAddress,
                DelayAsync = delayAsync
            });
            var token = await refreshClient.LoginAsync(username, password, cancellationToken);
            await secrets.SetAuthTokenAsync(token.Token);
            return token;
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await secrets.ClearAuthTokenAsync();
            OnSessionExpired();
            return null;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            // Vorübergehender Fehler: die ursprüngliche Anfrage scheitert
            // regulär — kein falscher Anmeldezwang bei Netzproblemen.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Stille Neuanmeldung fehlgeschlagen: {ex.Message}");
            return null;
        }
    }

    private void OnSessionExpired()
    {
        try
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] SessionExpired-Handler fehlgeschlagen: {ex.Message}");
        }
    }

    private static HttpClient CreateHttpClient(Uri baseAddress)
    {
        return new HttpClient { BaseAddress = baseAddress, Timeout = RequestTimeout };
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
