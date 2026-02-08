using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Yufanbot.Config.Test;

[TestFixture]
public class JsonConfigTests
{
    private class TestConfig1(
        ILogger<TestConfig1> logger, 
        IFileReader fileReader, 
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig1>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("int_variable", ConfigEntryGetType.FromConfigFile)]
        public int IntVariable { get; set; }

        [ConfigEntry("float_variable", ConfigEntryGetType.FromConfigFile)]
        public float FloatVariable { get; set; }

        [ConfigEntry("double_variable", ConfigEntryGetType.FromConfigFile)]
        public double DoubleVariable { get; set; }
        
        [ConfigEntry("long_variable", ConfigEntryGetType.FromConfigFile)]
        public long LongVariable { get; set; }
        
        [ConfigEntry("short_variable", ConfigEntryGetType.FromConfigFile)]
        public short ShortVariable { get; set; }

        [ConfigEntry("string_variable", ConfigEntryGetType.FromConfigFile)]
        public string StringVariable { get; set; } = "";

        [ConfigEntry("byte_variable", ConfigEntryGetType.FromConfigFile)]
        public byte ByteVariable { get; set; }

        [ConfigEntry("sbyte_variable", ConfigEntryGetType.FromConfigFile)]
        public sbyte SByteVariable { get; set; }

        [ConfigEntry("uint_variable", ConfigEntryGetType.FromConfigFile)]
        public uint UIntVariable { get; set; }

        [ConfigEntry("ulong_variable", ConfigEntryGetType.FromConfigFile)]
        public ulong ULongVariable { get; set; }

        [ConfigEntry("ushort_variable", ConfigEntryGetType.FromConfigFile)]
        public ushort UShortVariable { get; set; }

        [ConfigEntry("bool_variable", ConfigEntryGetType.FromConfigFile)]
        public bool BoolVariable { get; set; }

        [ConfigEntry("char_variable", ConfigEntryGetType.FromConfigFile)]
        public char CharVariable { get; set; }

        [ConfigEntry("decimal_variable", ConfigEntryGetType.FromConfigFile)]
        public decimal DecimalVariable { get; set; }
    }

    [Test]
    public void GetBasicVariableFromFile_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "int_variable": 1,
            "float_variable": 2.0,
            "double_variable": 3.0,
            "long_variable": 4,
            "short_variable": 5,
            "string_variable": "config",
            "byte_variable": 6,
            "sbyte_variable": 7,
            "uint_variable": 8,
            "ulong_variable": 9,
            "ushort_variable": 10,
            "bool_variable": true,
            "char_variable": "A",
            "decimal_variable": 11.5
        }
        """);
        
        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");
        
        var config = new TestConfig1(
            NullLogger<TestConfig1>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );
        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.IntVariable, Is.EqualTo(1));
            Assert.That(Math.Abs(config.FloatVariable - 2.0f), Is.LessThan(1e-5));
            Assert.That(Math.Abs(config.DoubleVariable - 3.0), Is.LessThan(1e-5));
            Assert.That(config.LongVariable, Is.EqualTo(4));
            Assert.That(config.ShortVariable, Is.EqualTo(5));
            Assert.That(config.StringVariable, Is.EqualTo("config"));
            Assert.That(config.ByteVariable, Is.EqualTo(6));
            Assert.That(config.SByteVariable, Is.EqualTo(7));
            Assert.That(config.UIntVariable, Is.EqualTo(8));
            Assert.That(config.ULongVariable, Is.EqualTo(9));
            Assert.That(config.UShortVariable, Is.EqualTo(10));
            Assert.That(config.BoolVariable, Is.True);
            Assert.That(config.CharVariable, Is.EqualTo('A'));
            Assert.That(config.DecimalVariable, Is.EqualTo(11.5m));
        }
    }

    private class TestConfig2(
        ILogger<TestConfig2> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig2>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("config", ConfigEntryGetType.FromConfigFile)]
        public ObjectConfig? Value { get; set; }

        public class ObjectConfig
        {
            public string Name { get; set; } = "";
            public int Variable { get; set; }
        }
    }

    [Test]
    public void GetComplexObjectFromFile_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "config": {
                "name": "test",
                "variable": 42
            }
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new TestConfig2(
            NullLogger<TestConfig2>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Value, Is.Not.Null);
            Assert.That(config.Value!.Name, Is.EqualTo("test"));
            Assert.That(config.Value.Variable, Is.EqualTo(42));
        }
    }

    private class TestConfig3(
        ILogger<TestConfig3> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig3>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("int_list", ConfigEntryGetType.FromConfigFile)]
        public List<int> IntList { get; set; } = [];

        [ConfigEntry("value_list", ConfigEntryGetType.FromConfigFile)]
        public List<Value> ValueList { get; set; } = [];

        public class Value
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }

    [Test]
    public void GetListVariablesFromFile_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("""
        {
            "int_list": [1, 2, 3, 4, 5],
            "value_list": [
                { "id": 1, "name": "first" },
                { "id": 2, "name": "second" },
                { "id": 3, "name": "third" }
            ]
        }
        """);

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        var config = new TestConfig3(
            NullLogger<TestConfig3>.Instance,
            fileReaderMock.Object,
            environmentMock.Object
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.IntList, Is.Not.Null);
            Assert.That(config.IntList, Has.Count.EqualTo(5));
            Assert.That(config.IntList[0], Is.EqualTo(1));
            Assert.That(config.IntList[1], Is.EqualTo(2));
            Assert.That(config.IntList[2], Is.EqualTo(3));
            Assert.That(config.IntList[3], Is.EqualTo(4));
            Assert.That(config.IntList[4], Is.EqualTo(5));

            Assert.That(config.ValueList, Is.Not.Null);
            Assert.That(config.ValueList, Has.Count.EqualTo(3));
            Assert.That(config.ValueList[0].Id, Is.EqualTo(1));
            Assert.That(config.ValueList[0].Name, Is.EqualTo("first"));
            Assert.That(config.ValueList[1].Id, Is.EqualTo(2));
            Assert.That(config.ValueList[1].Name, Is.EqualTo("second"));
            Assert.That(config.ValueList[2].Id, Is.EqualTo(3));
            Assert.That(config.ValueList[2].Name, Is.EqualTo("third"));
        }
    }
}
