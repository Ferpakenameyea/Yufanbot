using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void TestCompilePlugin_ShouldSuccess()
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
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Helloworld;

            public class HelloworldPlugin(ILogger<HelloworldPlugin> logger) : IPlugin
            {
                private readonly ILogger<HelloworldPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("Hello, world!");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public void TestCompilePlugin_NoMetaInf_ShouldReturnNull()
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
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Helloworld;

            public class HelloworldPlugin(ILogger<HelloworldPlugin> logger) : IPlugin
            {
                private readonly ILogger<HelloworldPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("Hello, world!");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_NoIPluginEntry_ShouldReturnNull()
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
            namespace Yufanbot.Plugin.NoEntry;

            public class NotAPlugin
            {
                public void DoSomething()
                {
                    System.Console.WriteLine("This is not a plugin entry");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_MultipleIPluginEntries_ShouldReturnNull()
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
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.Multiple;

            public class FirstPlugin(ILogger<FirstPlugin> logger) : IPlugin
            {
                private readonly ILogger<FirstPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("First plugin initialized");
                }
            }

            public class SecondPlugin(ILogger<SecondPlugin> logger) : IPlugin
            {
                private readonly ILogger<SecondPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("Second plugin initialized");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_MultipleSourceFiles_ShouldSuccess()
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
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.MultiSource;

            public class MultiSourcePlugin(ILogger<MultiSourcePlugin> logger) : IPlugin
            {
                private readonly ILogger<MultiSourcePlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation(Helper.GetMessage());
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public void TestCompilePlugin_InvalidJsonInMetaInf_ShouldReturnNull()
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
            using Microsoft.Extensions.Logging;
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.InvalidJson;

            public class InvalidJsonPlugin(ILogger<InvalidJsonPlugin> logger) : IPlugin
            {
                private readonly ILogger<InvalidJsonPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_CSharpSyntaxError_ShouldReturnNull()
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
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.SyntaxError;

            public class SyntaxErrorPlugin(ILogger<SyntaxErrorPlugin> logger) : IPlugin
            {
                private readonly ILogger<SyntaxErrorPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("This has a syntax error"
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_WrongFileExtension_ShouldReturnNull()
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
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.WrongExt;

            public class WrongExtensionPlugin(ILogger<WrongExtensionPlugin> logger) : IPlugin
            {
                private readonly ILogger<WrongExtensionPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_EmptyPlugin_ShouldReturnNull()
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
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_FileNotExist_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        string pluginPath = Path.Combine(".", "nonexistent.yf");
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_InvalidZipFormat_ShouldReturnNull()
    {
        PluginCompiler pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
        using WorkSpace outputWorkspace = new(".");
        string invalidZipPath = Path.Combine(outputWorkspace.DirectoryInfo.FullName, "invalid.yf");
        File.WriteAllText(invalidZipPath, "This is not a valid zip file");
        var plugin = pluginCompiler.CompilePlugin(invalidZipPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_MetaDataMissingRequiredFields_ShouldReturnNull()
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
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.MissingFields;

            public class MissingFieldsPlugin(ILogger<MissingFieldsPlugin> logger) : IPlugin
            {
                private readonly ILogger<MissingFieldsPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public void TestCompilePlugin_NoFileExtension_ShouldReturnNull()
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
            using Yufanbot.Plugin.Common;

            namespace Yufanbot.Plugin.NoExt;

            public class NoExtensionPlugin(ILogger<NoExtensionPlugin> logger) : IPlugin
            {
                private readonly ILogger<NoExtensionPlugin> _logger = logger;

                public void OnInitialize()
                {
                    _logger.LogInformation("This should not be reached");
                }
            }
            """);
        var plugin = pluginCompiler.CompilePlugin(pluginPath);
        Assert.That(plugin, Is.Null);
    }


}
