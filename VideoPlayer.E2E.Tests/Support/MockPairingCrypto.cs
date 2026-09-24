using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Server-Seite des Pairing-Handshake des Mock-Servers: spiegelt den
/// Vertrag von <c>api/pairing/exchange</c> — gemeinsamer Schlüssel per
/// ECDH P-256 (<c>DeriveKeyFromHmac</c>, SHA-256), AES-256-GCM mit dem
/// Payload-Layout <c>nonce(12) ‖ ciphertext ‖ tag(16)</c>. Deckungsgleich
/// mit <c>PairingCrypto</c> (App) und <c>PairingService</c> (Server-Repo).
/// </summary>
public static class MockPairingCrypto
{
    private const int NonceLength = 12;
    private const int TagLength = 16;

    /// <summary>
    /// Verschlüsselt das Geräte-Token für den anfragenden Client
    /// (<c>api/pairing/exchange</c> — Klartext-Token).
    /// </summary>
    /// <param name="clientPublicKeyBase64">Öffentlicher Client-Schlüssel (SPKI/DER, Base64).</param>
    /// <param name="deviceToken">Das auszustellende Geräte-Token (Klartext).</param>
    /// <returns>Öffentlicher Serverschlüssel und verschlüsseltes Token (je Base64).</returns>
    public static (string ServerPublicKey, string EncryptedToken) EncryptToken(
        string clientPublicKeyBase64, string deviceToken) =>
        EncryptPayload(clientPublicKeyBase64, deviceToken);

    /// <summary>
    /// Verschlüsselt ein beliebiges Paket für den anfragenden Client —
    /// beim Bootstrap (<c>api/pairing/bootstrap</c>) das JSON-Session-Paket
    /// mit Geräte-Token, Anmelde-Token, Ablauf und Refresh-Token.
    /// </summary>
    /// <param name="clientPublicKeyBase64">Öffentlicher Client-Schlüssel (SPKI/DER, Base64).</param>
    /// <param name="payload">Das zu verschlüsselnde Paket (Klartext).</param>
    /// <returns>Öffentlicher Serverschlüssel und verschlüsseltes Paket (je Base64).</returns>
    public static (string ServerPublicKey, string EncryptedPayload) EncryptPayload(
        string clientPublicKeyBase64, string payload)
    {
        using var serverKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var clientKey = ECDiffieHellman.Create();
        clientKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clientPublicKeyBase64), out _);
        var aesKey = serverKey.DeriveKeyFromHmac(
            clientKey.PublicKey, HashAlgorithmName.SHA256, null, null, null);

        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var plaintext = Encoding.UTF8.GetBytes(payload);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];
        using (var aes = new AesGcm(aesKey, TagLength))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var wire = new byte[NonceLength + ciphertext.Length + TagLength];
        nonce.CopyTo(wire, 0);
        ciphertext.CopyTo(wire, NonceLength);
        tag.CopyTo(wire, NonceLength + ciphertext.Length);

        return (
            Convert.ToBase64String(serverKey.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(wire));
    }
}
