using NUnit.Framework;

namespace Yufanbot.Plugin.Test;

[TestFixture]
public class NugetPackageStringTests
{
    [Test]
    public void ParseCorrectNugetString_ShouldSuccess()
    {
        var tuple = Nuget.ParsePackageString("Package:latest");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Package"));
            Assert.That(tuple.Value.version, Is.EqualTo("latest"));
        });
    }

    [Test]
    public void ParsePackageString_WithVersion_ShouldReturnNameAndVersion()
    {
        var tuple = Nuget.ParsePackageString("Newtonsoft.Json:13.0.3");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Newtonsoft.Json"));
            Assert.That(tuple.Value.version, Is.EqualTo("13.0.3"));
        });
    }

    [Test]
    public void ParsePackageString_WithoutVersion_ShouldReturnNameWithLatestVersion()
    {
        var tuple = Nuget.ParsePackageString("Serilog");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Serilog"));
            Assert.That(tuple.Value.version, Is.EqualTo("latest"));
        });
    }

    [Test]
    public void ParsePackageString_WithMultipleColons_ShouldReturnNull()
    {
        var tuple = Nuget.ParsePackageString("Package:version:extra");
        Assert.That(tuple, Is.Null);
    }

    [Test]
    public void ParsePackageString_EmptyString_ShouldReturnNull()
    {
        var tuple = Nuget.ParsePackageString("");
        Assert.That(tuple, Is.Null);
    }

    [Test]
    public void ParsePackageString_OnlyColon_ShouldReturnNull()
    {
        var tuple = Nuget.ParsePackageString(":");
        Assert.That(tuple, Is.Null);
    }

    [Test]
    public void ParsePackageString_WithWhitespace_ShouldTrim()
    {
        var tuple = Nuget.ParsePackageString(" Package : version ");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Package"));
            Assert.That(tuple.Value.version, Is.EqualTo("version"));
        });
    }

    [Test]
    public void ParsePackageString_OnlyNameWithWhitespace_ShouldTrimAndDefaultToLatest()
    {
        var tuple = Nuget.ParsePackageString(" Serilog ");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Serilog"));
            Assert.That(tuple.Value.version, Is.EqualTo("latest"));
        });
    }

    [Test]
    public void ParsePackageString_ComplexVersion_ShouldReturnCorrectVersion()
    {
        var tuple = Nuget.ParsePackageString("MyPackage:1.2.3-beta");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("MyPackage"));
            Assert.That(tuple.Value.version, Is.EqualTo("1.2.3-beta"));
        });
    }

    [Test]
    public void ParsePackageString_SpecialCharactersInName_ShouldHandleCorrectly()
    {
        var tuple = Nuget.ParsePackageString("My-Package_123:1.0.0");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("My-Package_123"));
            Assert.That(tuple.Value.version, Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void ParsePackageString_MixedCase_ShouldPreserveCase()
    {
        var tuple = Nuget.ParsePackageString("MyPackage:2.0.1-rc1");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("MyPackage"));
            Assert.That(tuple.Value.version, Is.EqualTo("2.0.1-rc1"));
        });
    }

    [Test]
    public void ParsePackageString_VersionWithPreRelease_ShouldReturnCorrectVersion()
    {
        var tuple = Nuget.ParsePackageString("Package:3.0.0-alpha.1");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Package"));
            Assert.That(tuple.Value.version, Is.EqualTo("3.0.0-alpha.1"));
        });
    }

    [Test]
    public void ParsePackageString_WhitespaceInMiddle_ShouldTrim()
    {
        var tuple = Nuget.ParsePackageString("  Package  :  1.0.0  ");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("Package"));
            Assert.That(tuple.Value.version, Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void ParsePackageString_SingleWhitespaceName_ShouldReturnNameWithLatest()
    {
        var tuple = Nuget.ParsePackageString("a");
        Assert.That(tuple, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(tuple.Value.name, Is.EqualTo("a"));
            Assert.That(tuple.Value.version, Is.EqualTo("latest"));
        });
    }
}