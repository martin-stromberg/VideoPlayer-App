using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppProfile</c>.</summary>
public class AppProfileTests
{
    /// <summary>Prüft FromEnvironment: liest das Profil aus der Umgebungsvariable.</summary>
    [Fact]
    public void FromEnvironment_ReadsProfileVariable()
    {
        var profile = AppProfile.FromEnvironment(name => name == AppProfile.EnvironmentVariable ? "Work" : null);

        Assert.Equal("work", profile.Name);
    }

    /// <summary>Prüft FromEnvironment: verwendet ohne gesetzte Umgebungsvariable das Standardprofil.</summary>
    [Fact]
    public void FromEnvironment_DefaultsToDefault()
    {
        var profile = AppProfile.FromEnvironment(_ => null);

        Assert.Equal(AppProfile.DefaultName, profile.Name);
    }

    /// <summary>Prüft Create: schreibt den Profilnamen klein und trimmt ihn.</summary>
    [Fact]
    public void Create_LowercasesAndTrims()
    {
        Assert.Equal("work", AppProfile.Create("  WORK ").Name);
    }

    /// <summary>Prüft Create: bei leerem oder Whitespace-Namen → verwendet das Standardprofil.</summary>
    [Fact]
    public void Create_EmptyOrWhitespace_DefaultsToDefault()
    {
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create("").Name);
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create("   ").Name);
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create(null).Name);
    }

    /// <summary>Prüft PrefixKey: beim Standardprofil → kein Präfix.</summary>
    [Fact]
    public void PrefixKey_DefaultProfile_NoPrefix()
    {
        Assert.Equal("SomeKey", AppProfile.Create("default").PrefixKey("SomeKey"));
    }

    /// <summary>Prüft PrefixKey: bei benanntem Profil → der Schlüssel wird präfixiert.</summary>
    [Fact]
    public void PrefixKey_NamedProfile_Prefixed()
    {
        Assert.Equal("work_SomeKey", AppProfile.Create("work").PrefixKey("SomeKey"));
    }

    /// <summary>Prüft DirectoryName: beim Standardprofil → leerer Verzeichnisname.</summary>
    [Fact]
    public void DirectoryName_DefaultProfile_Empty()
    {
        Assert.Equal(string.Empty, AppProfile.Create("default").DirectoryName);
    }

    /// <summary>Prüft DirectoryName: bei benanntem Profil → der Profilname ist der Verzeichnisname.</summary>
    [Fact]
    public void DirectoryName_NamedProfile_ProfileName()
    {
        Assert.Equal("work", AppProfile.Create("work").DirectoryName);
    }
}
