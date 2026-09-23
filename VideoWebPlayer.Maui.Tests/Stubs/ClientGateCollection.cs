namespace VideoWebPlayer.Maui.Tests.Stubs;

/// <summary>
/// Serialisiert alle Testklassen, die den statischen
/// <c>ClientGate.DeviceTokenProvider</c> lesen oder mutieren: der
/// veränderliche globale Zustand ist nicht thread-sicher gegenüber
/// parallelen Testklassen (Muster: <see cref="SqliteDatabaseCollection"/>).
/// </summary>
[CollectionDefinition("ClientGate")]
public sealed class ClientGateCollection;
