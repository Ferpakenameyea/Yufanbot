using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Yufanbot.Config;

namespace Yufanbot.Plugin.Test;

[TestFixture]
public class PluginCompileTests
{
    private ServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });
        services.AddSingleton(provider =>
        {
            var mock = new Mock<IConfigProvider>();
            mock.Setup(m => m.Resolve<PluginCompilerConfig>())
                .Returns(() =>
                {
                    Mock<IFileReader> reader = new();
                    reader.Setup(reader => reader.ReadAllText(It.IsAny<FileInfo>()))
                        .Returns("");

                    Mock<IEnvironmentVariableProvider> envProvider = new();
                    envProvider.Setup(provider => provider.GetEnvironmentVariable(It.IsAny<string>()))
                        .Returns(default(string));

                    return new PluginCompilerConfig(
                        NullLogger<PluginCompilerConfig>.Instance,
                        reader.Object,
                        envProvider.Object
                    );
                });
                
            return mock.Object;
        });
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    private static string CreatePlugin(
        WorkSpace outputWorkSpace,
        string? metaInfo,
        params Span<string> sources)
    {
        return CreatePluginWithSuffix(
            outputWorkSpace,
            metaInfo,
            ".yf",
            sources
        );
    }

    private static string CreatePluginWithSuffix(
        WorkSpace outputWorkSpace,
        string? metaInfo,
        string suffix,
        params Span<string> sources)
    {
        using WorkSpace inputWorkSpace = new(".");

        for (int i = 0; i < sources.Length; i++)
        {
            string sourcePath = Path.Combine(inputWorkSpace.DirectoryInfo.FullName, $"Plugin_source_{i}.cs");
            File.WriteAllText(sourcePath, sources[i]);
        }
        if (metaInfo != null)
        {
            string metaPath = Path.Combine(inputWorkSpace.DirectoryInfo.FullName, "META_INF");
            File.WriteAllText(metaPath, metaInfo);
        }

        string path = Path.Combine(outputWorkSpace.DirectoryInfo.FullName, $"Plugin{suffix}");

        ZipFile.CreateFromDirectory(
            inputWorkSpace.DirectoryInfo.FullName,
            path,
            CompressionLevel.Fastest,
            includeBaseDirectory: false);

        return path;
    }

    [Test]
    public async Task TestCompilePlugin_ShouldSuccess()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "helloworld",
                "name": "HelloWorld",
                "description": "HelloWorld plugin",
                "version": "1.0.0",
                "author": "mcdaxia"
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Helloworld;

            public class HelloworldPlugin(ILogger<HelloworldPlugin> logger) : IPlugin
            {
                private readonly ILogger<HelloworldPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("Hello, world!");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestCompilePlugin_NoMetaInf_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: null,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Helloworld;

            public class HelloworldPlugin(ILogger<HelloworldPlugin> logger) : IPlugin
            {
                private readonly ILogger<HelloworldPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("Hello, world!");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_NoIPluginEntry_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "noentry",
                "name": "NoEntryPlugin",
                "description": "Plugin without IPlugin entry",
                "version": "1.0.0"
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            namespace Yufanbot.Plugin.NoEntry;

            public class NotAPlugin
            {
                public void DoSomething()
                {
                    System.Console.WriteLine("This is not a plugin entry");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_MultipleIPluginEntries_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "multiple",
                "name": "MultipleEntriesPlugin",
                "description": "Plugin with multiple IPlugin entries",
                "version": "1.0.0"
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Multiple;

            public class FirstPlugin(ILogger<FirstPlugin> logger) : IPlugin
            {
                private readonly ILogger<FirstPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("First plugin initialized");
                }
            }

            public class SecondPlugin(ILogger<SecondPlugin> logger) : IPlugin
            {
                private readonly ILogger<SecondPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("Second plugin initialized");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_MultipleSourceFiles_ShouldSuccess()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "multisource",
                "name": "MultiSourcePlugin",
                "description": "Plugin with multiple source files",
                "version": "1.0.0"
            }
            """,
            
            """
            namespace Yufanbot.Plugin.MultiSource;

            public class Helper
            {
                public static string GetMessage()
                {
                    return "Hello from helper!";
                }
            }
            """,
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.MultiSource;

            public class MultiSourcePlugin(ILogger<MultiSourcePlugin> logger) : IPlugin
            {
                private readonly ILogger<MultiSourcePlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation(Helper.GetMessage());
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestCompilePlugin_InvalidJsonInMetaInf_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "invalidjson",
                "name": "InvalidJsonPlugin"
                "description": "Plugin with invalid JSON",
                "version": "1.0.0"
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.InvalidJson;

            public class InvalidJsonPlugin(ILogger<InvalidJsonPlugin> logger) : IPlugin
            {
                private readonly ILogger<InvalidJsonPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_CSharpSyntaxError_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "syntaxerror",
                "name": "SyntaxErrorPlugin",
                "description": "Plugin with C# syntax error",
                "version": "1.0.0"
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.SyntaxError;

            public class SyntaxErrorPlugin(ILogger<SyntaxErrorPlugin> logger) : IPlugin
            {
                private readonly ILogger<SyntaxErrorPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This has a syntax error"
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WrongFileExtension_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePluginWithSuffix(outputWorkspace,
            metaInfo: """
            {
                "id": "wrongext",
                "name": "WrongExtensionPlugin",
                "description": "Plugin with wrong file extension",
                "version": "1.0.0"
            }
            """,
            suffix: ".zip",
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.WrongExt;

            public class WrongExtensionPlugin(ILogger<WrongExtensionPlugin> logger) : IPlugin
            {
                private readonly ILogger<WrongExtensionPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_EmptyPlugin_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "empty",
                "name": "EmptyPlugin",
                "description": "Empty plugin with no source files",
                "version": "1.0.0"
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_FileNotExist_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        string pluginPath = Path.Combine(".", "nonexistent.yf");
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_InvalidZipFormat_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string invalidZipPath = Path.Combine(outputWorkspace.DirectoryInfo.FullName, "invalid.yf");
        File.WriteAllText(invalidZipPath, "This is not a valid zip file");
        var plugin = await pluginCompiler.CompilePluginAsync(invalidZipPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_MetaDataMissingRequiredFields_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "name": "MissingFieldsPlugin",
                "description": "Plugin missing required id field"
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.MissingFields;

            public class MissingFieldsPlugin(ILogger<MissingFieldsPlugin> logger) : IPlugin
            {
                private readonly ILogger<MissingFieldsPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_NoFileExtension_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePluginWithSuffix(outputWorkspace,
            metaInfo: """
            {
                "id": "noext",
                "name": "NoExtensionPlugin",
                "description": "Plugin with no file extension",
                "version": "1.0.0"
            }
            """,
            suffix: "",
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.NoExt;

            public class NoExtensionPlugin(ILogger<NoExtensionPlugin> logger) : IPlugin
            {
                private readonly ILogger<NoExtensionPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithValidNewtonsoftJsonDependency_ShouldSuccess()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "jsonparser",
                "name": "JsonParserPlugin",
                "description": "Plugin that uses Newtonsoft.Json",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:13.0.3"]
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Newtonsoft.Json;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.JsonParser;

            public class JsonParserPlugin(ILogger<JsonParserPlugin> logger) : IPlugin
            {
                private readonly ILogger<JsonParserPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    var data = new { message = "Hello from Newtonsoft.Json!" };
                    string json = JsonConvert.SerializeObject(data);
                    _logger.LogInformation("JSON serialized: {json}", json);
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithLatestVersionDependency_ShouldSuccess()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "jsonparserlatest",
                "name": "JsonParserLatestPlugin",
                "description": "Plugin that uses latest Newtonsoft.Json",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:latest"]
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Newtonsoft.Json;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.JsonParserLatest;

            public class JsonParserLatestPlugin(ILogger<JsonParserLatestPlugin> logger) : IPlugin
            {
                private readonly ILogger<JsonParserLatestPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    var data = new { message = "Using latest Newtonsoft.Json!" };
                    string json = JsonConvert.SerializeObject(data);
                    _logger.LogInformation("JSON: {json}", json);
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithInvalidPackageNameDependency_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "invaliddep",
                "name": "InvalidDependencyPlugin",
                "description": "Plugin with invalid package dependency",
                "version": "1.0.0",
                "nuget_dependencies": ["ThisPackageDoesNotExistForSure:1.0.0"]
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.InvalidDep;

            public class InvalidDependencyPlugin(ILogger<InvalidDependencyPlugin> logger) : IPlugin
            {
                private readonly ILogger<InvalidDependencyPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithInvalidVersionDependency_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "invalidver",
                "name": "InvalidVersionPlugin",
                "description": "Plugin with invalid version dependency",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:notarealversion999"]
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.InvalidVer;

            public class InvalidVersionPlugin(ILogger<InvalidVersionPlugin> logger) : IPlugin
            {
                private readonly ILogger<InvalidVersionPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithInvalidDependencyStringFormat_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "invalidformat",
                "name": "InvalidFormatPlugin",
                "description": "Plugin with invalid dependency string format",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:13.0.3:extra"]
            }
            """,
            sources:
            """
            using NapPlana.Core.Bot;
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.InvalidFormat;

            public class InvalidFormatPlugin(ILogger<InvalidFormatPlugin> logger) : IPlugin
            {
                private readonly ILogger<InvalidFormatPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestCompilePlugin_WithMultipleDependencies_ShouldSuccess()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "multidep",
                "name": "MultiDependencyPlugin",
                "description": "Plugin with multiple dependencies",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:13.0.3", "Serilog:latest"]
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Newtonsoft.Json;
            using Serilog;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.MultiDep;

            public class MultiDependencyPlugin(ILogger<MultiDependencyPlugin> logger) : IPlugin
            {
                private readonly ILogger<MultiDependencyPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    var data = new { message = "Multiple dependencies work!" };
                    string json = JsonConvert.SerializeObject(data);
                    _logger.LogInformation("Serialized: {json}", json);
                    Log.Information("Serilog also works!");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestCompilePlugin_OneInvalidAmongMultipleDependencies_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string pluginPath = CreatePlugin(outputWorkspace,
            metaInfo: """
            {
                "id": "oneinvalid",
                "name": "OneInvalidDependencyPlugin",
                "description": "Plugin with one invalid dependency among valid ones",
                "version": "1.0.0",
                "nuget_dependencies": ["Newtonsoft.Json:13.0.3", "ThisPackageDoesNotExist:1.0.0"]
            }
            """,
            sources:
            """
            using Microsoft.Extensions.Logging;
            using NapPlana.Core.Bot;
            using Newtonsoft.Json;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.OneInvalid;

            public class OneInvalidDependencyPlugin(ILogger<OneInvalidDependencyPlugin> logger) : IPlugin
            {
                private readonly ILogger<OneInvalidDependencyPlugin> _logger = logger;

                public void OnInitialize(NapBot bot)
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = await pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

}
