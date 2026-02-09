using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Yufanbot.Config.Test;

[TestFixture]
public class EnumConfigTests
{
    private class ConfigWithEnum(ILogger<ConfigWithEnum> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
        Config<ConfigWithEnum>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("enum", ConfigEntryGetType.FromEnvironment)]
        public EnumValue Value { get; set; } = EnumValue.V1;
    }

    private enum EnumValue
    {
        V1, V2, V3
    }

    [Test]
    public void ConfigWithEnum_ShouldConfigureCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("V2");

        var config = new ConfigWithEnum(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V2));
    }

    [Test]
    public void ConfigWithEnum_WhenNoMatch_ShouldUseDefault()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("VV");

        var config = new ConfigWithEnum(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V1));
    }

    [Test]
    public void ConfigWithEnum_IgnoreCase_ShouldConfigureCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("v3");

        var config = new ConfigWithEnum(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V3));
    }

    private class ConfigWithStrictEnum(ILogger<ConfigWithEnum> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
        Config<ConfigWithEnum>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("enum", ConfigEntryGetType.FromEnvironment)]
        [CaseMatch(CaseMatchMode.StrictCase)]
        public EnumValue Value { get; set; } = EnumValue.V1;
    }

    [Test]
    public void ConfigWithStrictEnum_WhenExactMatch_ShouldConfigureCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("V2");

        var config = new ConfigWithStrictEnum(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V2));
    }

    [Test]
    public void ConfigWithStrictEnum_WhenCaseMismatch_ShouldUseDefault()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("v2");

        var config = new ConfigWithStrictEnum(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V1));
    }

    private class ConfigWithExplicitIgnoreCase(ILogger<ConfigWithEnum> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
        Config<ConfigWithEnum>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("enum", ConfigEntryGetType.FromEnvironment)]
        [CaseMatch(CaseMatchMode.IgnoreCase)]
        public EnumValue Value { get; set; } = EnumValue.V1;
    }

    [Test]
    public void ConfigWithExplicitIgnoreCase_WhenCaseMismatch_ShouldConfigureCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("v2");

        var config = new ConfigWithExplicitIgnoreCase(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V2));
    }

    [Test]
    public void ConfigWithExplicitIgnoreCase_WhenMixedCase_ShouldConfigureCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        var environmentMock = new Mock<IEnvironmentVariableProvider>();

        environmentMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns("V3");

        var config = new ConfigWithExplicitIgnoreCase(
            NullLogger<ConfigWithEnum>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.Value, Is.EqualTo(EnumValue.V3));
    }
}