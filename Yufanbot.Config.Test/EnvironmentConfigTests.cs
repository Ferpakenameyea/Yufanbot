using Moq;
using Serilog;
using Serilog.Core;

namespace Yufanbot.Config.Test;

[TestFixture]
public class EnvironmentConfigTests
{
    private class TestConfig1(
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig1>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("int_variable", ConfigEntryGetType.FromEnvironment)]
        public int IntVariable { get; set; }

        [ConfigEntry("float_variable", ConfigEntryGetType.FromEnvironment)]
        public float FloatVariable { get; set; }

        [ConfigEntry("double_variable", ConfigEntryGetType.FromEnvironment)]
        public double DoubleVariable { get; set; }

        [ConfigEntry("long_variable", ConfigEntryGetType.FromEnvironment)]
        public long LongVariable { get; set; }

        [ConfigEntry("short_variable", ConfigEntryGetType.FromEnvironment)]
        public short ShortVariable { get; set; }

        [ConfigEntry("string_variable", ConfigEntryGetType.FromEnvironment)]
        public string StringVariable { get; set; } = "";

        [ConfigEntry("byte_variable", ConfigEntryGetType.FromEnvironment)]
        public byte ByteVariable { get; set; }

        [ConfigEntry("sbyte_variable", ConfigEntryGetType.FromEnvironment)]
        public sbyte SByteVariable { get; set; }

        [ConfigEntry("uint_variable", ConfigEntryGetType.FromEnvironment)]
        public uint UIntVariable { get; set; }

        [ConfigEntry("ulong_variable", ConfigEntryGetType.FromEnvironment)]
        public ulong ULongVariable { get; set; }

        [ConfigEntry("ushort_variable", ConfigEntryGetType.FromEnvironment)]
        public ushort UShortVariable { get; set; }

        [ConfigEntry("bool_variable", ConfigEntryGetType.FromEnvironment)]
        public bool BoolVariable { get; set; }

        [ConfigEntry("char_variable", ConfigEntryGetType.FromEnvironment)]
        public char CharVariable { get; set; }

        [ConfigEntry("decimal_variable", ConfigEntryGetType.FromEnvironment)]
        public decimal DecimalVariable { get; set; }
    }

    [Test]
    public void GetBasicVariableFromEnvironment_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable("int_variable")).Returns("1");
        environmentMock.Setup(m => m.GetEnvironmentVariable("float_variable")).Returns("2.0");
        environmentMock.Setup(m => m.GetEnvironmentVariable("double_variable")).Returns("3.0");
        environmentMock.Setup(m => m.GetEnvironmentVariable("long_variable")).Returns("4");
        environmentMock.Setup(m => m.GetEnvironmentVariable("short_variable")).Returns("5");
        environmentMock.Setup(m => m.GetEnvironmentVariable("string_variable")).Returns("config");
        environmentMock.Setup(m => m.GetEnvironmentVariable("byte_variable")).Returns("6");
        environmentMock.Setup(m => m.GetEnvironmentVariable("sbyte_variable")).Returns("7");
        environmentMock.Setup(m => m.GetEnvironmentVariable("uint_variable")).Returns("8");
        environmentMock.Setup(m => m.GetEnvironmentVariable("ulong_variable")).Returns("9");
        environmentMock.Setup(m => m.GetEnvironmentVariable("ushort_variable")).Returns("10");
        environmentMock.Setup(m => m.GetEnvironmentVariable("bool_variable")).Returns("true");
        environmentMock.Setup(m => m.GetEnvironmentVariable("char_variable")).Returns("A");
        environmentMock.Setup(m => m.GetEnvironmentVariable("decimal_variable")).Returns("11.5");

        var config = new TestConfig1(
            Logger.None,
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
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig2>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("config", ConfigEntryGetType.FromEnvironment)]
        public ObjectConfig? Value { get; set; }

        public class ObjectConfig
        {
            public string Name { get; set; } = "";
            public int Variable { get; set; }
        }
    }

    [Test]
    public void GetComplexObjectFromEnvironment_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable("config")).Returns("""
        {
            "name": "test",
            "variable": 42
        }
        """);

        var config = new TestConfig2(
            Logger.None,
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
        ILogger logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig3>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("int_list", ConfigEntryGetType.FromEnvironment)]
        public List<int> IntList { get; set; } = [];

        [ConfigEntry("value_list", ConfigEntryGetType.FromEnvironment)]
        public List<Value> ValueList { get; set; } = [];

        public class Value
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }

    [Test]
    public void GetListVariablesFromEnvironment_ShouldPresent()
    {
        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable("int_list")).Returns("[1, 2, 3, 4, 5]");
        environmentMock.Setup(m => m.GetEnvironmentVariable("value_list")).Returns("""
        [
            { "id": 1, "name": "first" },
            { "id": 2, "name": "second" },
            { "id": 3, "name": "third" }
        ]
        """);

        var config = new TestConfig3(
            Logger.None,
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