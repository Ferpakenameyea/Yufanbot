using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Yufanbot.Config;

namespace Yufanbot.Config.Test;

[TestFixture]
public class ConfigProviderTests
{
    private class TestConfig(
        ILogger<TestConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<TestConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("test_value", ConfigEntryGetType.FromConfigFile)]
        public string TestValue { get; set; } = "default";
    }

    private class AnotherTestConfig(
        ILogger<AnotherTestConfig> logger,
        IFileReader fileReader,
        IEnvironmentVariableProvider environmentVariableProvider) : Config<AnotherTestConfig>(logger, fileReader, environmentVariableProvider)
    {
        [ConfigEntry("another_value", ConfigEntryGetType.FromConfigFile)]
        public int AnotherValue { get; set; } = 42;
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        var fileReaderMock = new Mock<IFileReader>();
        fileReaderMock.Setup(m => m.ReadAllText(It.IsAny<FileInfo>())).Returns("{}");

        var environmentMock = new Mock<IEnvironmentVariableProvider>();
        environmentMock.Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns("");

        services.AddSingleton<IFileReader>(fileReaderMock.Object);
        services.AddSingleton<IEnvironmentVariableProvider>(environmentMock.Object);
        services.AddLogging();

        return services.BuildServiceProvider();
    }

    [Test]
    public void Resolve_GenericMethod_ShouldReturnValidConfigInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var config = provider.Resolve<TestConfig>();

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.InstanceOf<TestConfig>());
        Assert.That(config.TestValue, Is.EqualTo("default"));
    }

    [Test]
    public void Resolve_GenericMethod_ShouldReturnDifferentInstances()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var config1 = provider.Resolve<TestConfig>();
        var config2 = provider.Resolve<TestConfig>();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(config1, Is.Not.Null);
            Assert.That(config2, Is.Not.Null);
        });

        Assert.That(config1, Is.Not.SameAs(config2));
    }

    [Test]
    public void Resolve_GenericMethod_WithDifferentConfigTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var testConfig = provider.Resolve<TestConfig>();
        var anotherConfig = provider.Resolve<AnotherTestConfig>();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(testConfig, Is.InstanceOf<TestConfig>());
            Assert.That(anotherConfig, Is.InstanceOf<AnotherTestConfig>());
        });

        Assert.That(anotherConfig.AnotherValue, Is.EqualTo(42));
    }

    [Test]
    public void Resolve_NonGenericMethod_ShouldReturnValidConfigInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var config = provider.Resolve(typeof(TestConfig));

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.InstanceOf<TestConfig>());
        Assert.That(config, Is.InstanceOf<IConfig>());
    }

    [Test]
    public void Resolve_NonGenericMethod_ShouldReturnDifferentInstances()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var config1 = provider.Resolve(typeof(TestConfig));
        var config2 = provider.Resolve(typeof(TestConfig));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(config1, Is.Not.Null);
            Assert.That(config2, Is.Not.Null);
        });

        Assert.That(config1, Is.Not.SameAs(config2));
    }

    [Test]
    public void Resolve_NonGenericMethod_WithDifferentConfigTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act
        var testConfig = provider.Resolve(typeof(TestConfig));
        var anotherConfig = provider.Resolve(typeof(AnotherTestConfig));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(testConfig, Is.InstanceOf<TestConfig>());
            Assert.That(anotherConfig, Is.InstanceOf<AnotherTestConfig>());
        });

    }

    [Test]
    public void Resolve_WithNonConfigType_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        var ex = Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(string))
        );

        Assert.That(ex.Message, Does.Contain("not a config type"));
    }

    [Test]
    public void Resolve_WithNonIConfigType_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        var ex = Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(List<int>))
        );

        Assert.That(ex.Message, Does.Contain("not a config type"));
    }

    [Test]
    public void Resolve_GenericMethod_WithNonConfigType_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        var ex = Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(string))
        );

        Assert.That(ex.Message, Does.Contain("not a config type"));
    }

    [Test]
    public void Resolve_GenericMethod_WithIntType_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        var ex = Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(int))
        );

        Assert.That(ex.Message, Does.Contain("not a config type"));
    }

    [Test]
    public void Resolve_WithNullType_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(string))
        );
    }

    private class InvalidConfigType
    {
        public string SomeProperty { get; set; } = "value";
    }

    [Test]
    public void Resolve_WithClassNotDerivingFromConfig_ShouldThrowConfigResolveException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var provider = new ConfigProvider(serviceProvider);

        // Act & Assert
        var ex = Assert.Throws<ConfigResolveException>(() =>
            provider.Resolve(typeof(InvalidConfigType))
        );

        Assert.That(ex.Message, Does.Contain("InvalidConfigType"));
        Assert.That(ex.Message, Does.Contain("not a config type"));
    }
}
