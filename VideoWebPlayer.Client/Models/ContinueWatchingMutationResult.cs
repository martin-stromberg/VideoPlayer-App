namespace VideoWebPlayer.Client.Models;

/// <summary>
/// Ergebnis einer Änderung an der "Weiter ansehen"-Liste.
/// </summary>
/// <param name="Status">Status der Mutation (z. B. "ok").</param>
/// <param name="Message">Vom Server zurückgegebene Meldung.</param>
/// <returns>Eine neue Instanz, die das Ergebnis der Mutation beschreibt.</returns>
public sealed record ContinueWatchingMutationResult(string Status, string Message);
