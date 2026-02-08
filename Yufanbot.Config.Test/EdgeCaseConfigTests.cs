using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Yufanbot.Config.Test;

[TestFixture]
public class EdgeCaseConfigTests
{
    private class EmptyConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<EmptyConfig>(logger, fileReader, environmentVariableProvider)
    {
    }

    [Test]
    public void EmptyConfig_ShouldLoadSuccessfully()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new EmptyConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config, Is.Not.Null);
    }

    private class RequiredConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<RequiredConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("required_string", ConfigEntryGetType.FromConfigFile, optional: false)]
        public string RequiredString { get; set; } = "";

        [ConfigEntry("required_int", ConfigEntryGetType.FromConfigFile, optional: false)]
        public int RequiredInt { get; set; }

        [ConfigEntry("required_bool", ConfigEntryGetType.FromConfigFile, optional: false)]
        public bool RequiredBool { get; set; }
    }

    [Test]
    public void MissingRequiredConfigItems_ShouldUseDefaults()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new RequiredConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.RequiredString, Is.EqualTo(""));
            Assert.That(config.RequiredInt, Is.EqualTo(0));
            Assert.That(config.RequiredBool, Is.False);
        }
    }

    [Test]
    public void PartiallyMissingRequiredConfigItems_ShouldUseDefaultsForMissing()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "required_string": "present",
            "required_int": 42
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new RequiredConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.RequiredString, Is.EqualTo("present"));
            Assert.That(config.RequiredInt, Is.EqualTo(42));
            Assert.That(config.RequiredBool, Is.False);
        }
    }

    private class OptionalConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<OptionalConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("optional_string", ConfigEntryGetType.FromConfigFile)]
        public string OptionalString { get; set; } = "default_string";

        [ConfigEntry("optional_int", ConfigEntryGetType.FromConfigFile)]
        public int OptionalInt { get; set; } = 100;

        [ConfigEntry("optional_bool", ConfigEntryGetType.FromConfigFile)]
        public bool OptionalBool { get; set; } = true;

        [ConfigEntry("optional_list", ConfigEntryGetType.FromConfigFile)]
        public List<int> OptionalList { get; set; } = new() { 1, 2, 3 };
    }

    [Test]
    public void MissingOptionalConfigItems_ShouldUseDefaults()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new OptionalConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.OptionalString, Is.EqualTo("default_string"));
            Assert.That(config.OptionalInt, Is.EqualTo(100));
            Assert.That(config.OptionalBool, Is.True);
            Assert.That(config.OptionalList, Is.Not.Null);
            Assert.That(config.OptionalList, Has.Count.EqualTo(3));
            Assert.That(config.OptionalList[0], Is.EqualTo(1));
            Assert.That(config.OptionalList[1], Is.EqualTo(2));
            Assert.That(config.OptionalList[2], Is.EqualTo(3));
        }
    }

    [Test]
    public void PartiallyPresentOptionalConfigItems_ShouldOverrideDefaults()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "optional_string": "overridden",
            "optional_bool": false
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new OptionalConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.OptionalString, Is.EqualTo("overridden"));
            Assert.That(config.OptionalInt, Is.EqualTo(100));
            Assert.That(config.OptionalBool, Is.False);
            Assert.That(config.OptionalList, Is.Not.Null);
            Assert.That(config.OptionalList, Has.Count.EqualTo(3));
        }
    }

    private class NullableConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<NullableConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("nullable_string", ConfigEntryGetType.FromConfigFile)]
        public string? NullableString { get; set; }

        [ConfigEntry("nullable_int", ConfigEntryGetType.FromConfigFile)]
        public int? NullableInt { get; set; }

        [ConfigEntry("nullable_object", ConfigEntryGetType.FromConfigFile)]
        public NestedObject? NullableObject { get; set; }

        public class NestedObject
        {
            public string Name { get; set; } = "";
        }
    }

    [Test]
    public void MissingNullableConfigItems_ShouldBeNull()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new NullableConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.NullableString, Is.Null);
            Assert.That(config.NullableInt, Is.Null);
            Assert.That(config.NullableObject, Is.Null);
        }
    }

    [Test]
    public void PresentNullableConfigItems_ShouldHaveValues()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "nullable_string": "not null",
            "nullable_int": 42,
            "nullable_object": {
                "name": "test"
            }
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new NullableConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.NullableString, Is.EqualTo("not null"));
            Assert.That(config.NullableInt, Is.EqualTo(42));
            Assert.That(config.NullableObject, Is.Not.Null);
            Assert.That(config.NullableObject!.Name, Is.EqualTo("test"));
        }
    }

    private class InvalidTypeConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<InvalidTypeConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("int_from_string", ConfigEntryGetType.FromConfigFile)]
        public int IntFromString { get; set; }
    }

    [Test]
    public void InvalidTypeConversion_ShouldHandleGracefully()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "int_from_string": "not a number"
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new InvalidTypeConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.IntFromString, Is.EqualTo(0));
    }

    private class EmptyFileConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<EmptyFileConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("some_value", ConfigEntryGetType.FromConfigFile)]
        public string SomeValue { get; set; } = "default";
    }

    [Test]
    public void EmptyConfigFileContent_ShouldHandleGracefully()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new EmptyFileConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.SomeValue, Is.EqualTo("default"));
    }

    private class IOExceptionConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<IOExceptionConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("some_value", ConfigEntryGetType.FromConfigFile)]
        public string SomeValue { get; set; } = "default";
    }

    [Test]
    public void IOExceptionDuringFileRead_ShouldUseDefaultValue()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Throws(new IOException("Cannot read file"));

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new IOExceptionConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.SomeValue, Is.EqualTo("default"));
    }

    private class InvalidJsonConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<InvalidJsonConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("some_value", ConfigEntryGetType.FromConfigFile)]
        public string SomeValue { get; set; } = "default";
    }

    [Test]
    public void InvalidJsonString_ShouldHandleGracefully()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{ invalid json content }");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new InvalidJsonConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.SomeValue, Is.EqualTo("default"));
    }

    private class NestedPathConfig(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<NestedPathConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("part.path.value", ConfigEntryGetType.FromConfigFile)]
        public string NestedValue { get; set; } = "default";
    }

    [Test]
    public void MissingNestedPath_ShouldUseDefault()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "part": {
                "other_key": "some_value"
            }
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new NestedPathConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.NestedValue, Is.EqualTo("default"));
    }

    [Test]
    public void MissingParentObjectInNestedPath_ShouldUseDefault()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "other_part": {
                "path": {
                    "value": "some_value"
                }
            }
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new NestedPathConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.NestedValue, Is.EqualTo("default"));
    }

    [Test]
    public void ValidNestedPath_ShouldRetrieveValue()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "part": {
                "path": {
                    "value": "nested_value"
                }
            }
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new NestedPathConfig(
            NullLogger.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        Assert.That(config.NestedValue, Is.EqualTo("nested_value"));
    }
}