namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// Ein Discovery-Weg (z. B. mDNS oder UDP-Broadcast), der Server-Kandidaten
/// im lokalen Netzwerk liefert. Fehler eines Wegs werden von der
/// Implementierung geschluckt und als leere Trefferliste gemeldet —
/// Discovery darf die Einrichtung nie blockieren.
/// </summary>
public interface IServerDiscovery
{
    /// <summary>Sucht Server-Kandidaten; liefert eine (ggf. leere) Liste.</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Gefundene Server-Kandidaten (ggf. leer).</returns>
    Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken cancellationToken = default);
}
