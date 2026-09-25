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
    private readonly DeviceTokenService deviceTokens;
    private readonly Func<Uri, HttpClient> httpClientFactory;
    private readonly Func<TimeSpan, CancellationToken, Task>? delayAsync;

    /// <summary>Erstellt den Dienst mit der produktiven <see cref="HttpClient"/>-Erzeugung.</summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    public AuthSessionService(AppSecretsService secrets)
        : this(secrets, new DeviceTokenService(secrets), null, null)
    {
    }

    /// <summary>
    /// Erstellt den Dienst mit einer injizierbaren <see cref="HttpClient"/>-Fabrik
    /// und optionaler Warte-Implementierung für die Wiederholungsstrategie
    /// (für Tests; produktiv wird ein Client mit Basisadresse und Timeout erzeugt).
    /// Der <see cref="DeviceTokenService"/> teilt die Fabrik für seine
    /// Gate-Probe.
    /// </summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    /// <param name="httpClientFactory">Fabrik für <see cref="HttpClient"/> pro Serveradresse; <c>null</c> = produktive Erzeugung mit Timeout.</param>
    /// <param name="delayAsync">Optionale Warte-Implementierung der Wiederholungsstrategie (Tests); <c>null</c> = <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    public AuthSessionService(
        AppSecretsService secrets,
        Func<Uri, HttpClient>? httpClientFactory,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
        : this(secrets, new DeviceTokenService(secrets, httpClientFactory), httpClientFactory, delayAsync)
    {
    }

    /// <summary>
    /// Erstellt den Dienst mit Geräte-Token-Verwaltung sowie injizierbarer
    /// <see cref="HttpClient"/>-Fabrik und Warte-Implementierung (Tests).
    /// </summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    /// <param name="deviceTokens">Verwaltung der gepaarten Geräte-Tokens (Gate-Probe, Widerruf).</param>
    /// <param name="httpClientFactory">Fabrik für <see cref="HttpClient"/> pro Serveradresse; <c>null</c> = produktive Erzeugung mit Timeout.</param>
    /// <param name="delayAsync">Optionale Warte-Implementierung der Wiederholungsstrategie (Tests); <c>null</c> = <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    public AuthSessionService(
        AppSecretsService secrets,
        DeviceTokenService deviceTokens,
        Func<Uri, HttpClient>? httpClientFactory = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.deviceTokens = deviceTokens ?? throw new ArgumentNullException(nameof(deviceTokens));
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
    /// nutzt zuerst ein noch gültiges gespeichertes Token (direkter
    /// Einstieg ohne Anmeldemaske), sonst die gespeicherten Zugangsdaten;
    /// <see cref="AuthAttemptStatus.MissingCredentials"/>, wenn beides
    /// fehlt (z. B. nach einer Backup-Wiederherstellung — Geheimnisse
    /// gelangen nicht ins Cloud-Backup).
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

        // Direkter Einstieg: ein noch gültiges Token erspart die
        // Anmeldemaske; ein abgelaufenes Token fällt auf die
        // Zugangsdaten zurück.
        var storedToken = await secrets.GetAuthTokenAsync();
        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            var tokenAttempt = await TryStoredTokenAsync(serverAddress, storedToken, cancellationToken);
            if (tokenAttempt is not null)
            {
                return tokenAttempt with { Username = EmptyToNull(username) };
            }
        }

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
    /// Beendet die Sitzung („Abmelden"): der gehaltene API-Client wird
    /// freigegeben, ein vorhandenes Refresh-Token wird best-effort
    /// serverseitig widerrufen (<c>api/auth/logout</c>) und die
    /// gespeicherten Zugangsdaten samt Token und Refresh-Token werden
    /// entfernt — danach ist keine automatische Anmeldung mehr möglich.
    /// </summary>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Räumen der Sitzung.</returns>
    public async Task LogoutAsync()
    {
        var baseAddress = ClientBaseAddress;
        Client = null;
        ClientBaseAddress = null;
        await RevokeRefreshTokenAsync(baseAddress);
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

    /// <summary>
    /// Widerruft das gespeicherte Refresh-Token best-effort am Server
    /// (<c>api/auth/logout</c>): Fehler werden nur protokolliert — die
    /// lokale Abmeldung hängt nicht am Serverkontakt, und der Server
    /// behandelt bereits ungültige Token idempotent.
    /// </summary>
    /// <param name="serverAddress">Adresse der zu beendenden Sitzung; <c>null</c>/ungültig → kein Aufruf.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Widerrufsversuch.</returns>
    private async Task RevokeRefreshTokenAsync(string? serverAddress)
    {
        try
        {
            var refreshToken = await secrets.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken)
                || !ServerAddress.TryNormalize(serverAddress, out var normalized))
            {
                return;
            }

            using var http = httpClientFactory(new Uri(normalized));
            var client = new VideoPlayerApiClient(http, new VideoPlayerApiClientOptions
            {
                BaseAddress = new Uri(normalized),
                DelayAsync = delayAsync
            });
            await client.LogoutSessionAsync(refreshToken, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Best effort: ein unerreichbarer Server oder ein bereits
            // ungültiges Token verhindert die lokale Abmeldung nicht.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Refresh-Token-Widerruf fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Direkter Einstieg über ein gespeichertes Anmelde-Token: der Token
    /// wird mit einer leichtgewichtigen authentifizierten Anfrage
    /// (<c>api/favorites</c>) validiert. Ein abgelaufenes Token kann die
    /// verdrahtete stille Neuanmeldung dabei direkt erneuern.
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers.</param>
    /// <param name="token">Das gespeicherte Anmelde-Token.</param>
    /// <param name="cancellationToken">Bricht die Validierung ab.</param>
    /// <returns>
    /// Das Ergebnis des Versuchs; <c>null</c>, wenn der Token abgelehnt
    /// wurde und der Zugangsdaten-Pfad greifen soll.
    /// </returns>
    private async Task<AuthAttempt?> TryStoredTokenAsync(
        Uri serverAddress,
        string token,
        CancellationToken cancellationToken)
    {
        var client = CreateClient(serverAddress);
        client.SetAccessToken(new AuthToken { Token = token });
        try
        {
            await client.GetFavoritesAsync(cancellationToken);
            Client = client;
            ClientBaseAddress = serverAddress.ToString();
            return new AuthAttempt(AuthAttemptStatus.Succeeded);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Das 401/403 kann vom Client-Gate (widerrufenes Geräte-Token)
            // oder vom Anmelde-Token stammen — die Gate-Probe trennt beide
            // Fälle. Bei einer Gate-Ablehnung entfällt der Zugangsdaten-
            // Fallback: auch eine Anmeldung käme nicht am Gate vorbei.
            if (await HandleGateRejectionAsync(serverAddress, cancellationToken))
            {
                return new AuthAttempt(AuthAttemptStatus.GateRejected);
            }

            // Token endgültig abgelehnt → verwerfen und auf die
            // Zugangsdaten zurückfallen.
            await secrets.ClearAuthTokenAsync();
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            // Transport-/Serverfehler → der Aufrufer erhält
            // „nicht erreichbar" und weicht auf den Offline-Pfad aus.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Token-Validierung fehlgeschlagen: {ex.Message}");
            return new AuthAttempt(AuthAttemptStatus.ServerUnreachable);
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
            // Das 401/403 kann vom Client-Gate (widerrufenes Geräte-Token)
            // oder von den Zugangsdaten stammen — die Gate-Probe trennt
            // beide Fälle (Muster: SilentReloginAsync).
            return await HandleGateRejectionAsync(serverAddress, cancellationToken)
                ? new AuthAttempt(AuthAttemptStatus.GateRejected, username)
                : new AuthAttempt(AuthAttemptStatus.InvalidCredentials, username);
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
    /// Stille Neuanmeldung: zuerst der Refresh-Token der Sitzung
    /// (<c>api/auth/refresh</c>, Bootstrap-Sitzungen besitzen keine
    /// Zugangsdaten), dann die gespeicherten Zugangsdaten — jeweils über
    /// einen eigenen kurzlebigen Client (vermeidet Rekursion im
    /// aufrufenden Request). Bei fehlenden oder abgelehnten
    /// Erneuerungswegen wird <see cref="SessionExpired"/> ausgelöst und
    /// <c>null</c> geliefert; bei vorübergehenden Fehlern nur <c>null</c>.
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers, an dem neu angemeldet wird.</param>
    /// <param name="cancellationToken">Bricht die Neuanmeldung ab.</param>
    /// <returns>Das neue Anmelde-Token; <c>null</c> bei Fehlschlag.</returns>
    private async Task<AuthToken?> SilentReloginAsync(Uri serverAddress, CancellationToken cancellationToken)
    {
        // Erneuerung ohne Zugangsdaten: das rotierte Paket wird
        // vollständig persistiert; ein endgültig abgelehnter Refresh-Token
        // wird verworfen und der Zugangsdaten-Pfad greift danach.
        var refreshed = await TryRefreshSessionAsync(serverAddress, cancellationToken);
        if (refreshed is not null)
        {
            return refreshed;
        }

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
            // Das 401/403 kann vom Client-Gate (widerrufenes Geräte-Token)
            // oder von den Benutzer-Zugangsdaten stammen — die Gate-Probe
            // mit nur dem Gate-Header trennt beide Fälle.
            if (await HandleGateRejectionAsync(serverAddress, cancellationToken))
            {
                return null;
            }

            await secrets.ClearAuthTokenAsync();
            OnSessionExpired();
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            // Vorübergehender Fehler: die ursprüngliche Anfrage scheitert
            // regulär — kein falscher Anmeldezwang bei Netzproblemen.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Stille Neuanmeldung fehlgeschlagen: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Versucht die Sitzungserneuerung über das gespeicherte Refresh-Token
    /// (<c>api/auth/refresh</c>): bei Erfolg werden das neue Anmelde-Token
    /// und der rotierte Refresh-Token persistiert. Ein 401/403 wird per
    /// Gate-Probe aufgetrennt — Gerätewiderruf bleibt Gerätewiderruf; ein
    /// endgültig abgelehntes Refresh-Token wird verworfen und der
    /// Zugangsdaten-Pfad darf danach greifen.
    /// </summary>
    /// <param name="serverAddress">Adresse des Servers, an dem erneuert wird.</param>
    /// <param name="cancellationToken">Bricht die Erneuerung ab.</param>
    /// <returns>Das neue Anmelde-Token; <c>null</c>, wenn kein Refresh-Weg greift.</returns>
    private async Task<AuthToken?> TryRefreshSessionAsync(Uri serverAddress, CancellationToken cancellationToken)
    {
        string? refreshToken;
        try
        {
            refreshToken = await secrets.GetRefreshTokenAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Refresh-Token nicht lesbar: {ex.Message}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            using var http = httpClientFactory(serverAddress);
            var refreshClient = new VideoPlayerApiClient(http, new VideoPlayerApiClientOptions
            {
                BaseAddress = serverAddress,
                DelayAsync = delayAsync
            });
            var session = await refreshClient.RefreshSessionAsync(refreshToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(session.Token) || string.IsNullOrWhiteSpace(session.RefreshToken))
            {
                // Unvollständige Rotation würde die Sitzung korrumpieren —
                // lieber auf den Zugangsdaten-Pfad zurückfallen.
                return null;
            }

            await secrets.SetAuthTokenAsync(session.Token);
            await secrets.SetRefreshTokenAsync(session.RefreshToken);
            return new AuthToken { Token = session.Token, Expires = session.Expires };
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Das 401/403 kann vom Client-Gate (widerrufenes Geräte-Token)
            // oder vom Refresh-Token stammen — die Gate-Probe trennt beide
            // Fälle (Muster: SilentReloginAsync-Credentials-Pfad).
            if (await HandleGateRejectionAsync(serverAddress, cancellationToken))
            {
                return null;
            }

            // Refresh-Token endgültig abgelehnt → verwerfen; danach greift
            // der Zugangsdaten-Pfad der stillen Neuanmeldung.
            try
            {
                await secrets.ClearRefreshTokenAsync();
            }
            catch (Exception clearEx)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Refresh-Token konnte nicht verworfen werden: {clearEx.Message}");
            }

            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            // Vorübergehender Fehler: der Zugangsdaten-Pfad darf es noch
            // versuchen — schadet nicht und erhöht die Erfolgschance.
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Sitzungserneuerung per Refresh-Token fehlgeschlagen: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Klärt eine 401/403-Ablehnung per Gate-Probe
    /// (<see cref="DeviceTokenService.IsGateRejectedAsync"/>): Lehnt das
    /// Client-Gate den Schlüssel ab, wird das Geräte-Token der geprüften
    /// Adresse verworfen. Provider-Rücksetzung und Widerrufs-Meldung
    /// erfolgen nur, wenn der abgelehnte Server noch der aktuelle ist —
    /// ein zwischenzeitlicher Serverwechsel darf weder das frisch
    /// aktivierte Token des neuen Servers verwerfen noch die App
    /// fälschlich in die Re-Pairing-Eingabe zwingen.
    /// </summary>
    /// <param name="serverAddress">Adresse des ablehnenden Servers.</param>
    /// <param name="cancellationToken">Bricht die Probe ab.</param>
    /// <returns><c>true</c>, wenn das Gate den Schlüssel abgelehnt hat.</returns>
    private async Task<bool> HandleGateRejectionAsync(Uri serverAddress, CancellationToken cancellationToken)
    {
        var address = serverAddress.ToString();
        if (!await deviceTokens.IsGateRejectedAsync(address, cancellationToken))
        {
            return false;
        }

        // Das abgelehnte Geräte-Token gehört zur geprüften Adresse — es
        // wird gezielt gelöscht, auch wenn der Server nicht mehr aktiv ist.
        try
        {
            await secrets.ClearDeviceTokenAsync(address);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthSessionService] Geräte-Token konnte nicht verworfen werden: {ex.Message}");
        }

        if (await deviceTokens.DiscardIfCurrentAsync(address))
        {
            deviceTokens.NotifyRejected();
        }

        return true;
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
