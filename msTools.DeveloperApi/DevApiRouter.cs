#if DEBUG || DEV_API
namespace msTools.DeveloperApi;

/// <summary>
/// Routen-Tabelle der Developer-API: bildet (Methode, Pfad) auf einen
/// Handler ab und liefert feste 404-/405-Antworten für unbekannte Pfade
/// bzw. falsche Methoden.
/// </summary>
public sealed class DevApiRouter
{
    private readonly List<Route> routes = [];

    /// <summary>Registriert einen Handler für Methode und Pfad.</summary>
    /// <param name="method">HTTP-Methode (Groß-/Kleinschreibung egal).</param>
    /// <param name="path">Absoluter Pfad (z. B. <c>/model</c>).</param>
    /// <param name="handler">Der auszuführende Handler.</param>
    public void Map(string method, string path, DevApiEndpointHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(handler);
        routes.Add(new Route(method.ToUpperInvariant(), path, handler));
    }

    /// <summary>
    /// Führt den passenden Handler aus. Pfad bekannt, Methode falsch →
    /// <c>405</c>; Pfad unbekannt → <c>404</c>.
    /// </summary>
    /// <param name="request">Die eingehende Anfrage.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die zu versendende Antwort.</returns>
    public async Task<DevApiResponse> RouteAsync(DevApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = request.Path.Length > 1 ? request.Path.TrimEnd('/') : request.Path;
        foreach (var route in routes)
        {
            if (string.Equals(route.Path, path, StringComparison.Ordinal)
                && string.Equals(route.Method, request.Method, StringComparison.OrdinalIgnoreCase))
            {
                return await route.Handler(request, cancellationToken);
            }
        }

        if (routes.Any(r => string.Equals(r.Path, path, StringComparison.Ordinal)))
        {
            return DevApiResponse.Error(405, "Die Methode ist für diesen Endpunkt nicht erlaubt.");
        }

        return DevApiResponse.Error(404, "Der Endpunkt ist nicht bekannt.");
    }

    private sealed record Route(
        string Method,
        string Path,
        DevApiEndpointHandler Handler);
}
#endif
