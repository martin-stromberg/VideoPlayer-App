using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Kryptografische Bausteine des Geräte-Pairings: flüchtiges
/// ECDH-Schlüsselpaar (NIST P-256), SPKI/DER-Export, Ableitung des
/// gemeinsamen AES-256-Schlüssels per <c>DeriveKeyFromHmac</c> (SHA-256)
/// sowie AES-256-GCM-Entschlüsselung des Payload-Layouts
/// <c>nonce(12) ‖ ciphertext ‖ tag(16)</c>.
/// </summary>
internal static class PairingCrypto
{
    /// <summary>Länge des AES-GCM-Nonce in Bytes.</summary>
    internal const int NonceLength = 12;

    /// <summary>Länge des AES-GCM-Tags in Bytes.</summary>
    internal const int TagLength = 16;

    /// <summary>Erzeugt ein flüchtiges ECDH-Schlüsselpaar auf NIST P-256.</summary>
    /// <returns>Das erzeugte Schlüsselpaar (Aufrufer verwirft es via <c>using</c>).</returns>
    internal static ECDiffieHellman CreateKeyPair() =>
        ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

    /// <summary>Exportiert den öffentlichen Schlüssel als SPKI/DER, Base64-kodiert.</summary>
    /// <param name="keyPair">Das Geräte-Schlüsselpaar.</param>
    /// <returns>Base64-kodierter SPKI/DER-Schlüssel.</returns>
    internal static string ExportPublicKeyBase64(ECDiffieHellman keyPair)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        return Convert.ToBase64String(keyPair.ExportSubjectPublicKeyInfo());
    }

    /// <summary>
    /// Leitet den gemeinsamen AES-256-Schlüssel ab und entschlüsselt das
    /// Geräte-Token aus dem Payload <c>nonce(12) ‖ ciphertext ‖ tag(16)</c>.
    /// </summary>
    /// <param name="keyPair">Das flüchtige Geräte-Schlüsselpaar.</param>
    /// <param name="serverPublicKeyBase64">Öffentlicher Serverschlüssel (SPKI/DER, Base64).</param>
    /// <param name="encryptedTokenBase64">Verschlüsseltes Geräte-Token (Base64).</param>
    /// <returns>Das entschlüsselte Geräte-Token.</returns>
    /// <exception cref="CryptographicException">Ungültiges oder nicht entschlüsselbares Paket.</exception>
    internal static string DecryptToken(ECDiffieHellman keyPair, string serverPublicKeyBase64, string encryptedTokenBase64)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverPublicKeyBase64);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedTokenBase64);

        byte[] serverPublicKey;
        byte[] payload;
        try
        {
            serverPublicKey = Convert.FromBase64String(serverPublicKeyBase64);
            payload = Convert.FromBase64String(encryptedTokenBase64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Pairing-Antwort ist nicht Base64-kodiert.", ex);
        }

        if (payload.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("Pairing-Antwort ist zu kurz (Nonce/Tag fehlen).");
        }

        byte[] sharedKey;
        try
        {
            using var serverKey = ECDiffieHellman.Create();
            serverKey.ImportSubjectPublicKeyInfo(serverPublicKey, out _);
            sharedKey = keyPair.DeriveKeyFromHmac(
                serverKey.PublicKey,
                HashAlgorithmName.SHA256,
                secretPrepend: null,
                secretAppend: null,
                hmacKey: null);
        }
        catch (CryptographicException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Der Server-Schlüssel der Pairing-Antwort ist ungültig.", ex);
        }

        var nonce = payload.AsSpan(0, NonceLength);
        var tag = payload.AsSpan(payload.Length - TagLength, TagLength);
        var ciphertext = payload.AsSpan(NonceLength, payload.Length - NonceLength - TagLength);
        var plaintext = new byte[ciphertext.Length];

        using (var aes = new AesGcm(sharedKey, TagLength))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
