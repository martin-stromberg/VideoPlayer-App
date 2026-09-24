using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>BootstrapTicketParser</c>: die Ticket-Adresse des
/// Kopplungs-QR-Codes (<c>https://server[/pfad]/pairing?t=ticket</c>)
/// wird in Server-Basisadresse und opakes Ticket zerlegt; die
/// Kurzcode-Form geht ohne Serverangabe durch.
/// </summary>
public class BootstrapTicketParserTests
{
    /// <summary>Prüft TryParse: Root-Adresse — Basis ohne Unterpfad, Ticket unverändert.</summary>
    [Fact]
    public void TryParse_UrlAtRoot_ReturnsBaseAndTicket()
    {
        var ok = BootstrapTicketParser.TryParse(
            "https://nas.local/pairing?t=abc123geheim",
            out var result);

        Assert.True(ok);
        Assert.Equal(BootstrapTicketKind.Url, result!.Kind);
        Assert.Equal("https://nas.local/", result.ServerAddress);
        Assert.Equal("abc123geheim", result.Ticket);
    }

    /// <summary>Prüft TryParse: Unterpfad bleibt erhalten — nur das pairing-Segment fällt weg.</summary>
    /// <param name="url">Die Ticket-Adresse mit Unterpfad.</param>
    /// <param name="expectedBase">Die erwartete kanonische Basisadresse.</param>
    [Theory]
    [InlineData("https://nas.local/videoplayer/pairing?t=ticket", "https://nas.local/videoplayer/")]
    [InlineData("https://nas.local/a/b/pairing?t=ticket", "https://nas.local/a/b/")]
    [InlineData("https://nas.local:8443/videoplayer/pairing?t=ticket", "https://nas.local:8443/videoplayer/")]
    [InlineData("http://192.168.1.5:8096/pairing?t=ticket", "http://192.168.1.5:8096/")]
    [InlineData("https://nas.local/pairing/?t=ticket", "https://nas.local/")] // Trailing Slash
    [InlineData("https://NAS.LOCAL/Pairing?t=ticket", "https://nas.local/")] // Case-insensitiv
    public void TryParse_UrlWithSubpath_KeepsSubpath(string url, string expectedBase)
    {
        var ok = BootstrapTicketParser.TryParse(url, out var result);

        Assert.True(ok);
        Assert.Equal(expectedBase, result!.ServerAddress);
        Assert.Equal("ticket", result.Ticket);
    }

    /// <summary>Prüft TryParse: das Ticket wird opak und dekodiert übernommen — nie normalisiert.</summary>
    [Fact]
    public void TryParse_UrlTicket_IsOpaqueAndDecoded()
    {
        var ok = BootstrapTicketParser.TryParse(
            "https://nas/pairing?t=AbC-xYz%2F123%3D",
            out var result);

        Assert.True(ok);
        Assert.Equal("AbC-xYz/123=", result!.Ticket);
    }

    /// <summary>Prüft TryParse: der t-Parameter wird auch zwischen anderen Parametern gefunden.</summary>
    [Fact]
    public void TryParse_UrlTicketAmongOtherParams_FindsTicket()
    {
        var ok = BootstrapTicketParser.TryParse(
            "https://nas/pairing?x=1&t=mein-ticket&y=2",
            out var result);

        Assert.True(ok);
        Assert.Equal("mein-ticket", result!.Ticket);
    }

    /// <summary>Prüft TryParse: Kurzcode ohne URL-Anteil → ShortCode ohne Serveradresse.</summary>
    /// <param name="code">Der Kurzcode.</param>
    [Theory]
    [InlineData("A1B2C3D4")]
    [InlineData("x7k9")]
    [InlineData("abcdefgh")]
    public void TryParse_ShortCode_ReturnsShortCodeKind(string code)
    {
        var ok = BootstrapTicketParser.TryParse(code, out var result);

        Assert.True(ok);
        Assert.Equal(BootstrapTicketKind.ShortCode, result!.Kind);
        Assert.Null(result.ServerAddress);
        Assert.Equal(code, result.Ticket);
    }

    /// <summary>Prüft TryParse: umgebende Leerzeichen werden getrimmt.</summary>
    [Fact]
    public void TryParse_InputWithWhitespace_IsTrimmed()
    {
        var ok = BootstrapTicketParser.TryParse("  A1B2C3D4  ", out var result);

        Assert.True(ok);
        Assert.Equal("A1B2C3D4", result!.Ticket);
    }

    /// <summary>Prüft TryParse: unverwertbare Eingaben werden abgelehnt.</summary>
    /// <param name="input">Die abzulehnende Eingabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://nas/pairing?t=x")]           // kein http(s)
    [InlineData("https://nas/other?t=x")]            // kein /pairing
    [InlineData("https://nas/pairing/extra?t=x")]    // pairing nicht letztes Segment
    [InlineData("https://nas/pairing")]              // fehlendes t
    [InlineData("https://nas/pairing?t=")]           // leeres t
    [InlineData("https://nas/pairing?x=1")]          // t fehlt zwischen anderen
    [InlineData("https://nas/pairing?t=   ")]        // t nur Leerzeichen
    [InlineData("code mit leerzeichen")]             // Kurzcode mit Whitespace
    [InlineData("server:8096")]                      // Adress-Bruchstück als Kurzcode
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        var ok = BootstrapTicketParser.TryParse(input, out var result);

        Assert.False(ok);
        Assert.Null(result);
    }
}
