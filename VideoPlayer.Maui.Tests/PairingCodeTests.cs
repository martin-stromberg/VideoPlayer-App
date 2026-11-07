using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>PairingCode</c> (Normalisierung und Formatprüfung).</summary>
public class PairingCodeTests
{
    /// <summary>Prüft TryNormalize: akzeptiert einen wohlgeformten Code unverändert.</summary>
    [Fact]
    public void TryNormalize_ValidCode_Succeeds()
    {
        Assert.True(PairingCode.TryNormalize("ABCD2345", out var normalized));
        Assert.Equal("ABCD2345", normalized);
    }

    /// <summary>
    /// Prüft TryNormalize: akzeptiert serverseitig konfigurierbare Längen —
    /// die Code-Länge ist eine Server-Option (Pairing:CodeLength, min. 4),
    /// die lokale Prüfung darf sie nicht überstimmen.
    /// </summary>
    /// <param name="input">Die Roheingabe.</param>
    /// <param name="expected">Der erwartete normalisierte Code.</param>
    [Theory]
    [InlineData("ABCD", "ABCD")]
    [InlineData("ABC234", "ABC234")]
    [InlineData("ABCD2345", "ABCD2345")]
    [InlineData("ABCD23456789", "ABCD23456789")]
    public void TryNormalize_AcceptsServerConfiguredLengths(string input, string expected)
    {
        Assert.True(PairingCode.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    /// <summary>Prüft TryNormalize: trimmt und wandelt in Großbuchstaben um.</summary>
    /// <param name="input">Die Roheingabe.</param>
    /// <param name="expected">Der erwartete normalisierte Code.</param>
    [Theory]
    [InlineData("abcd2345", "ABCD2345")]
    [InlineData("  ABCD2345  ", "ABCD2345")]
    [InlineData(" kmnpqrtx ", "KMNPQRTX")]
    public void TryNormalize_NormalizesCaseAndWhitespace(string input, string expected)
    {
        Assert.True(PairingCode.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    /// <summary>Prüft TryNormalize: lehnt leere Eingaben ab.</summary>
    /// <param name="input">Die Roheingabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_RejectsEmpty(string? input)
    {
        Assert.False(PairingCode.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    /// <summary>Prüft TryNormalize: lehnt Eingaben unter der Mindestlänge ab.</summary>
    /// <param name="input">Die Roheingabe.</param>
    [Theory]
    [InlineData("A")]
    [InlineData("ABC")]
    public void TryNormalize_RejectsBelowMinLength(string input)
    {
        Assert.False(PairingCode.TryNormalize(input, out _));
    }

    /// <summary>
    /// Prüft TryNormalize: lehnt Zeichen außerhalb des Alphabets ab —
    /// einschließlich der bewusst ausgenommenen verwechselbaren Zeichen
    /// (I, L, O, 0, 1).
    /// </summary>
    /// <param name="input">Die Roheingabe.</param>
    [Theory]
    [InlineData("ABCD234I")]
    [InlineData("ABCD234L")]
    [InlineData("ABCD234O")]
    [InlineData("ABCD2340")]
    [InlineData("ABCD2341")]
    [InlineData("ABCD23-5")]
    [InlineData("ABCD2ä45")]
    public void TryNormalize_RejectsCharactersOutsideAlphabet(string input)
    {
        Assert.False(PairingCode.TryNormalize(input, out _));
    }

    /// <summary>Prüft Alphabet: enthält keine verwechselbaren Zeichen; Mindestlänge entspricht dem Server-Minimum.</summary>
    [Fact]
    public void Alphabet_ExcludesAmbiguousCharacters()
    {
        Assert.Equal(4, PairingCode.MinLength);
        Assert.DoesNotContain('I', PairingCode.Alphabet);
        Assert.DoesNotContain('L', PairingCode.Alphabet);
        Assert.DoesNotContain('O', PairingCode.Alphabet);
        Assert.DoesNotContain('0', PairingCode.Alphabet);
        Assert.DoesNotContain('1', PairingCode.Alphabet);
    }
}
