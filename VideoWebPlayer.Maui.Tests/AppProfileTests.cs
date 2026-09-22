using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

public class AppProfileTests
{
    [Fact]
    public void FromEnvironment_ReadsProfileVariable()
    {
        var profile = AppProfile.FromEnvironment(name => name == AppProfile.EnvironmentVariable ? "Work" : null);

        Assert.Equal("work", profile.Name);
    }

    [Fact]
    public void FromEnvironment_DefaultsToDefault()
    {
        var profile = AppProfile.FromEnvironment(_ => null);

        Assert.Equal(AppProfile.DefaultName, profile.Name);
    }

    [Fact]
    public void Create_LowercasesAndTrims()
    {
        Assert.Equal("work", AppProfile.Create("  WORK ").Name);
    }

    [Fact]
    public void Create_EmptyOrWhitespace_DefaultsToDefault()
    {
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create("").Name);
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create("   ").Name);
        Assert.Equal(AppProfile.DefaultName, AppProfile.Create(null).Name);
    }

    [Fact]
    public void PrefixKey_DefaultProfile_NoPrefix()
    {
        Assert.Equal("SomeKey", AppProfile.Create("default").PrefixKey("SomeKey"));
    }

    [Fact]
    public void PrefixKey_NamedProfile_Prefixed()
    {
        Assert.Equal("work_SomeKey", AppProfile.Create("work").PrefixKey("SomeKey"));
    }

    [Fact]
    public void DirectoryName_DefaultProfile_Empty()
    {
        Assert.Equal(string.Empty, AppProfile.Create("default").DirectoryName);
    }

    [Fact]
    public void DirectoryName_NamedProfile_ProfileName()
    {
        Assert.Equal("work", AppProfile.Create("work").DirectoryName);
    }
}
