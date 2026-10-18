using System.Security.Cryptography;
using System.Text;
using VideoPlayer.ApiClient;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>PairingCrypto</c>: ECDH-Ableitung (P-256, SPKI/DER, SHA-256)
/// und AES-256-GCM-Entschlüsselung des Payload-Layouts
/// <c>nonce(12) ‖ ciphertext ‖ tag(16)</c> inklusive Fehlerfälle.
/// </summary>
public class PairingCryptoTests
{
    /// <summary>
    /// Simuliert den Server: importiert den öffentlichen Geräte-Schlüssel,
    /// leitet den gemeinsamen Schlüssel ab und verschlüsselt das Token im
    /// vertraglichen Payload-Layout.
    /// </summary>
    /// <param name="clientPublicKeyBase64">Öffentlicher Geräte-Schlüssel (SPKI/DER, Base64).</param>
    /// <param name="token">Das zu verschlüsselnde Geräte-Token.</param>
    /// <returns>Öffentlicher Server-Schlüssel und verschlüsseltes Token (jeweils Base64).</returns>
    private static (string ServerPublicKey, string EncryptedToken) SimulateServer(
        string clientPublicKeyBase64,
        string token)
    {
        using var serverKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var clientKey = ECDiffieHellman.Create();
        clientKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clientPublicKeyBase64), out _);

        var sharedKey = serverKey.DeriveKeyFromHmac(
            clientKey.PublicKey, HashAlgorithmName.SHA256, null, null, null);

        var plaintext = Encoding.UTF8.GetBytes(token);
        var nonce = RandomNumberGenerator.GetBytes(PairingCrypto.NonceLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[PairingCrypto.TagLength];
        using (var aes = new AesGcm(sharedKey, PairingCrypto.TagLength))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var payload = nonce.Concat(ciphertext).Concat(tag).ToArray();
        return (
            Convert.ToBase64String(serverKey.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(payload));
    }

    /// <summary>Prüft den Roundtrip: Server-Verschlüsselung → Gerät entschlüsselt das Token.</summary>
    [Fact]
    public void DecryptToken_Roundtrip_RecoversPlaintextToken()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();
        var clientPublicKey = PairingCrypto.ExportPublicKeyBase64(keyPair);
        var (serverPublicKey, encryptedToken) = SimulateServer(clientPublicKey, "device-token-42");

        var decrypted = PairingCrypto.DecryptToken(keyPair, serverPublicKey, encryptedToken);

        Assert.Equal("device-token-42", decrypted);
    }

    /// <summary>Prüft ExportPublicKeyBase64: liefert einen importierbaren SPKI/DER-Schlüssel.</summary>
    [Fact]
    public void ExportPublicKeyBase64_ProducesImportableSpki()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();

        var exported = PairingCrypto.ExportPublicKeyBase64(keyPair);

        var bytes = Convert.FromBase64String(exported);
        using var imported = ECDiffieHellman.Create();
        imported.ImportSubjectPublicKeyInfo(bytes, out _);
        Assert.NotNull(imported.PublicKey);
    }

    /// <summary>Prüft DecryptToken: nicht-Base64-Eingaben → CryptographicException.</summary>
    [Fact]
    public void DecryptToken_InvalidBase64_Throws()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();
        var (serverPublicKey, _) = SimulateServer(PairingCrypto.ExportPublicKeyBase64(keyPair), "t");

        Assert.Throws<CryptographicException>(
            () => PairingCrypto.DecryptToken(keyPair, "!!!kein-base64!!!", serverPublicKey));
        Assert.Throws<CryptographicException>(
            () => PairingCrypto.DecryptToken(keyPair, serverPublicKey, "!!!kein-base64!!!"));
    }

    /// <summary>Prüft DecryptToken: zu kurzes Paket (Nonce/Tag fehlen) → CryptographicException.</summary>
    [Fact]
    public void DecryptToken_TooShortPayload_Throws()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();
        var (serverPublicKey, _) = SimulateServer(PairingCrypto.ExportPublicKeyBase64(keyPair), "t");
        var shortPayload = Convert.ToBase64String(new byte[PairingCrypto.NonceLength + PairingCrypto.TagLength - 1]);

        Assert.Throws<CryptographicException>(
            () => PairingCrypto.DecryptToken(keyPair, serverPublicKey, shortPayload));
    }

    /// <summary>Prüft DecryptToken: manipuliertes Paket (Tag passt nicht) → CryptographicException.</summary>
    [Fact]
    public void DecryptToken_TamperedPayload_Throws()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();
        var (serverPublicKey, encryptedToken) = SimulateServer(
            PairingCrypto.ExportPublicKeyBase64(keyPair), "device-token-42");
        var payload = Convert.FromBase64String(encryptedToken);
        payload[^1] ^= 0xFF; // Tag-Bit kippen → GCM-Authentifizierung schlägt fehl.

        Assert.ThrowsAny<CryptographicException>(
            () => PairingCrypto.DecryptToken(keyPair, serverPublicKey, Convert.ToBase64String(payload)));
    }

    /// <summary>Prüft DecryptToken: anderer Server-Schlüssel (falsches Shared Secret) → CryptographicException.</summary>
    [Fact]
    public void DecryptToken_WrongServerKey_Throws()
    {
        using var keyPair = PairingCrypto.CreateKeyPair();
        var (_, encryptedToken) = SimulateServer(PairingCrypto.ExportPublicKeyBase64(keyPair), "t");
        using var otherServerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var wrongPublicKey = Convert.ToBase64String(otherServerKey.ExportSubjectPublicKeyInfo());

        Assert.ThrowsAny<CryptographicException>(
            () => PairingCrypto.DecryptToken(keyPair, wrongPublicKey, encryptedToken));
    }
}
