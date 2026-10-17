namespace VideoWebPlayer.Maui.Tests.Stubs;

/// <summary>
/// Serialisiert alle Testklassen, die reale SQLite-Datenbanken benutzen:
/// <c>SQLiteAsyncConnection.ResetPool()</c> im Dispose ist nicht
/// thread-sicher gegenüber parallelen Verbindungen anderer Testklassen.
/// </summary>
[CollectionDefinition("SqliteDatabase")]
public sealed class SqliteDatabaseCollection;
