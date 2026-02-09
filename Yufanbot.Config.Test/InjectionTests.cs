using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Yufanbot.Config.Test;

[TestFixture]
public class InjectionTests
{
    private class StringInjectionConfig(
        ILogger<StringInjectionConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<StringInjectionConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("normal_string", ConfigEntryGetType.FromConfigFile)]
        public string NormalString { get; set; } = "";

        [ConfigEntry("string_with_double_quotes", ConfigEntryGetType.FromConfigFile)]
        public string StringWithDoubleQuotes { get; set; } = "";

        [ConfigEntry("string_with_single_quotes", ConfigEntryGetType.FromConfigFile)]
        public string StringWithSingleQuotes { get; set; } = "";

        [ConfigEntry("string_with_backslash", ConfigEntryGetType.FromConfigFile)]
        public string StringWithBackslash { get; set; } = "";

        [ConfigEntry("string_with_newline", ConfigEntryGetType.FromConfigFile)]
        public string StringWithNewline { get; set; } = "";

        [ConfigEntry("string_with_tab", ConfigEntryGetType.FromConfigFile)]
        public string StringWithTab { get; set; } = "";

        [ConfigEntry("string_with_mixed_quotes", ConfigEntryGetType.FromConfigFile)]
        public string StringWithMixedQuotes { get; set; } = "";

        [ConfigEntry("string_with_json_escape", ConfigEntryGetType.FromConfigFile)]
        public string StringWithJsonEscape { get; set; } = "";
    }

    [Test]
    public void StringInjection_ShouldHandleQuotesCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "normal_string": "hello world",
            "string_with_double_quotes": "he said \"hello\"",
            "string_with_single_quotes": "it's working",
            "string_with_backslash": "path\\to\\file",
            "string_with_newline": "line1\nline2",
            "string_with_tab": "col1\tcol2",
            "string_with_mixed_quotes": "she said \"it's working\"",
            "string_with_json_escape": "escaped \"quote\" and \\backslash"
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new StringInjectionConfig(
            NullLogger<StringInjectionConfig>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.NormalString, Is.EqualTo("hello world"));
            Assert.That(config.StringWithDoubleQuotes, Is.EqualTo("he said \"hello\""));
            Assert.That(config.StringWithSingleQuotes, Is.EqualTo("it's working"));
            Assert.That(config.StringWithBackslash, Is.EqualTo("path\\to\\file"));
            Assert.That(config.StringWithNewline, Is.EqualTo("line1\nline2"));
            Assert.That(config.StringWithTab, Is.EqualTo("col1\tcol2"));
            Assert.That(config.StringWithMixedQuotes, Is.EqualTo("she said \"it's working\""));
            Assert.That(config.StringWithJsonEscape, Is.EqualTo("escaped \"quote\" and \\backslash"));
        }
    }

    private class CharInjectionConfig(
        ILogger<CharInjectionConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<CharInjectionConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("normal_char", ConfigEntryGetType.FromConfigFile)]
        public char NormalChar { get; set; }

        [ConfigEntry("char_as_double_quote", ConfigEntryGetType.FromConfigFile)]
        public char CharAsDoubleQuote { get; set; }

        [ConfigEntry("char_as_single_quote", ConfigEntryGetType.FromConfigFile)]
        public char CharAsSingleQuote { get; set; }

        [ConfigEntry("char_as_backslash", ConfigEntryGetType.FromConfigFile)]
        public char CharAsBackslash { get; set; }

        [ConfigEntry("char_as_space", ConfigEntryGetType.FromConfigFile)]
        public char CharAsSpace { get; set; }

        [ConfigEntry("char_as_newline", ConfigEntryGetType.FromConfigFile)]
        public char CharAsNewline { get; set; }
    }

    [Test]
    public void CharInjection_ShouldHandleSpecialCharsCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "normal_char": "A",
            "char_as_double_quote": "\"",
            "char_as_single_quote": "'",
            "char_as_backslash": "\\",
            "char_as_space": " ",
            "char_as_newline": "\n"
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new CharInjectionConfig(
            NullLogger<CharInjectionConfig>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.NormalChar, Is.EqualTo('A'));
            Assert.That(config.CharAsDoubleQuote, Is.EqualTo('"'));
            Assert.That(config.CharAsSingleQuote, Is.EqualTo('\''));
            Assert.That(config.CharAsBackslash, Is.EqualTo('\\'));
            Assert.That(config.CharAsSpace, Is.EqualTo(' '));
            Assert.That(config.CharAsNewline, Is.EqualTo('\n'));
        }
    }

    private class EnvironmentStringInjectionConfig(
        ILogger<EnvironmentStringInjectionConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<EnvironmentStringInjectionConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("env_string_normal", ConfigEntryGetType.FromEnvironment)]
        public string EnvStringNormal { get; set; } = "";

        [ConfigEntry("env_string_with_quotes", ConfigEntryGetType.FromEnvironment)]
        public string EnvStringWithQuotes { get; set; } = "";

        [ConfigEntry("env_string_with_backslash", ConfigEntryGetType.FromEnvironment)]
        public string EnvStringWithBackslash { get; set; } = "";
    }

    [Test]
    public void EnvironmentStringInjection_ShouldHandleQuotesCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_string_normal")).Returns("normal value");
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_string_with_quotes")).Returns("value with \"quotes\"");
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_string_with_backslash")).Returns("path\\to\\file");

        var config = new EnvironmentStringInjectionConfig(
            NullLogger<EnvironmentStringInjectionConfig>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.EnvStringNormal, Is.EqualTo("normal value"));
            Assert.That(config.EnvStringWithQuotes, Is.EqualTo("value with \"quotes\""));
            Assert.That(config.EnvStringWithBackslash, Is.EqualTo("path\\to\\file"));
        }
    }

    private class EnvironmentCharInjectionConfig(
        ILogger<EnvironmentCharInjectionConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<EnvironmentCharInjectionConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("env_char_normal", ConfigEntryGetType.FromEnvironment)]
        public char EnvCharNormal { get; set; }

        [ConfigEntry("env_char_quote", ConfigEntryGetType.FromEnvironment)]
        public char EnvCharQuote { get; set; }

        [ConfigEntry("env_char_single_quote", ConfigEntryGetType.FromEnvironment)]
        public char EnvCharSingleQuote { get; set; }
    }

    [Test]
    public void EnvironmentCharInjection_ShouldHandleSpecialCharsCorrectly()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_char_normal")).Returns("A");
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_char_quote")).Returns("\"");
        environmentMock.Setup(m => m.GetEnvironmentVariable("env_char_single_quote")).Returns("'");

        var config = new EnvironmentCharInjectionConfig(
            NullLogger<EnvironmentCharInjectionConfig>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.EnvCharNormal, Is.EqualTo('A'));
            Assert.That(config.EnvCharQuote, Is.EqualTo('"'));
            Assert.That(config.EnvCharSingleQuote, Is.EqualTo('\''));
        }
    }
}