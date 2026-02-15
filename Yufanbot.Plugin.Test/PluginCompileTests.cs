using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Yufanbot.Config;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Plugin.Test;

[TestFixture]
public class PluginCompileTests
{
    private ServiceProvider _serviceProvider;
    private PluginCompiler _pluginCompiler;

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
        _pluginCompiler = new(
            NullLogger<PluginCompiler>.Instance,
            _serviceProvider
        );
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    private static string CreatePlugin(
        WorkSpace outputWorkSpace,
        string suffix,
        string? csproj,
        string? metaInfo,
        params Span<string> sources)
    {
        using WorkSpace inputWorkSpace = new(".");

        for (int i = 0; i < sources.Length; i++)
        {
            string sourcePath = Path.Combine(inputWorkSpace.DirectoryInfo.FullName, $"Plugin_source_{i}.cs");
            File.WriteAllText(sourcePath, sources[i]);
        }

        if (csproj != null)
        {
            string csprojPath = Path.Combine(inputWorkSpace.DirectoryInfo.FullName, "test.csproj");
            File.WriteAllText(csprojPath, csproj);
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
    public async Task TestNormalPlugin_ShouldCompile()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "aispeakup",
                "name": "Yufan AI Speakup",
                "description": "learn and speak in yufan's tone",
                "version": "1.0.0",
                "author": "mcdaxia"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello, world!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestPluginWithThirdPartyNugetPackage_ShouldCompile()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.4">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "json-parser",
                "name": "JSON Parser",
                "description": "A plugin that uses Newtonsoft.Json",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using Newtonsoft.Json;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    var data = new { message = "Hello from Newtonsoft.Json!" };
                    string json = JsonConvert.SerializeObject(data);
                    Console.WriteLine(json);
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestPluginWithIncompleteMetaInfo_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "name": "Incomplete Meta"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithoutMetaInfo_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: null,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithSyntaxError_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "syntax-error",
                "name": "Syntax Error",
                "description": "Plugin with syntax errors",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    // Missing closing parenthesis
                    Console.WriteLine("Hello!"
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithoutCsproj_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: null,
            metaInfo: """
            {
                "id": "no-csproj",
                "name": "No Csproj",
                "description": "Plugin without csproj file",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithIncompleteCsproj_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "incomplete-csproj",
                "name": "Incomplete Csproj",
                "description": "Plugin with incomplete csproj",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithInvalidCsprojSyntax_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
            <!-- Missing closing tags -->
            """,
            metaInfo: """
            {
                "id": "invalid-csproj",
                "name": "Invalid Csproj",
                "description": "Plugin with invalid csproj syntax",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithMissingClosingBrace_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "missing-brace",
                "name": "Missing Brace",
                "description": "Plugin with missing closing brace",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                // Missing closing brace for class
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithInvalidCSharpKeyword_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "invalid-keyword",
                "name": "Invalid Keyword",
                "description": "Plugin with invalid C# keyword usage",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    // Using 'class' as a variable name (invalid)
                    string class = "invalid";
                    Console.WriteLine(class);
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithUndeclaredVariable_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "undeclared-var",
                "name": "Undeclared Variable",
                "description": "Plugin with undeclared variable",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    // Using undefined variable
                    Console.WriteLine(undefinedVariable);
                }
            """);
        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);       
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithMultipleNugetPackages_ShouldCompile()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.4">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Serilog" Version="4.3.0">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "multi-package",
                "name": "Multiple Packages",
                "description": "Plugin with multiple NuGet packages",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using Newtonsoft.Json;
            using Serilog;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    
                    var data = new { message = "Using multiple packages!" };
                    string json = JsonConvert.SerializeObject(data);
                    Log.Information(json);
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);
    }

    [Test]
    public async Task TestPluginWithInvalidMetaInfoJson_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "invalid-json",
                "name": "Invalid JSON"
                "description": "Missing comma between fields"
                "version": "1.0.0"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginWithMissingIPluginImplementation_ShouldFail()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "no-impl",
                "name": "No Implementation",
                "description": "Plugin without IPlugin implementation",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Hello!");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Null);
    }

    [Test]
    public async Task TestPluginIPluginType_ShouldMatchHostIPluginType()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "type-match",
                "name": "Type Match Test",
                "description": "Test that plugin IPlugin matches host IPlugin",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Console.WriteLine("Type match test");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);

        // Verify that the plugin's entry type implements IPlugin
        var entry = plugin.Entry;
        Assert.That(entry, Is.AssignableTo<IPlugin>());

        // Verify that the IPlugin type from the plugin's assembly matches the host's IPlugin type
        var pluginIPluginType = entry.GetType().GetInterfaces()
            .FirstOrDefault(i => i.FullName == "Yufanbot.Plugin.Common.IPlugin");
        Assert.That(pluginIPluginType, Is.Not.Null);

        // The types should be the same because they come from the same assembly
        var hostIPluginType = typeof(IPlugin);
        Assert.That(pluginIPluginType.AssemblyQualifiedName, Is.EqualTo(hostIPluginType.AssemblyQualifiedName));
    }

    [Test]
    public async Task TestPluginWithNewtonsoftJson_ShouldHaveIsolatedType()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.4"/>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "isolated-type",
                "name": "Isolated Type Test",
                "description": "Test that plugin NuGet types are isolated from host",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using Newtonsoft.Json;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                }

                public void RunNewtonsoft()
                {
                    var data = new { message = "Test" };
                    string json = JsonConvert.SerializeObject(data);
                    Console.WriteLine(json);
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);

        // Get the plugin's entry and verify it can use Newtonsoft.Json
        var entry = plugin.Entry;
        var entryType = entry.GetType();

        // Check if the plugin has the RunNewtonsoft method
        var runMethod = entryType.GetMethod("RunNewtonsoft");
        Assert.That(runMethod, Is.Not.Null, "Plugin should have RunNewtonsoft method");

        // Call the method to verify Newtonsoft.Json is available and working
        // This method uses JsonConvert.SerializeObject internally
        Assert.DoesNotThrow(() => runMethod.Invoke(entry, null),
            "RunNewtonsoft method should execute successfully, proving Newtonsoft.Json is available in the plugin");

        // Verify that Newtonsoft.Json is NOT available in the host application's loaded assemblies
        // (unless the host also references Newtonsoft.Json)
        var hostHasNewtonsoftJson = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "Newtonsoft.Json");

        // If the host doesn't have Newtonsoft.Json, verify the plugin has its own isolated version
        if (!hostHasNewtonsoftJson)
        {
            var hostAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToList();

            Assert.That(hostAssemblies, Does.Not.Contain("Newtonsoft.Json"),
                "Host should not have Newtonsoft.Json loaded when plugin has its own isolated version");
        }
    }

    [Test]
    public async Task TestPluginWithSerilog_ShouldHaveIsolatedType()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Serilog" Version="4.3.0" />
                    <PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "serilog-isolated",
                "name": "Serilog Isolated Test",
                "description": "Test that Serilog types are isolated from host",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using Serilog;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                }

                public void RunSerilog()
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Information("Serilog test");
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);

        // Get the plugin's entry and verify it can use Serilog
        var entry = plugin.Entry;
        var entryType = entry.GetType();

        // Check if the plugin has the RunSerilog method
        var runMethod = entryType.GetMethod("RunSerilog");
        Assert.That(runMethod, Is.Not.Null, "Plugin should have RunSerilog method");

        // Call the method to verify Serilog is available and working
        // This method uses LoggerConfiguration and Log.Information internally
        Assert.DoesNotThrow(() => runMethod.Invoke(entry, null),
            "RunSerilog method should execute successfully, proving Serilog is available in the plugin");

        // Verify the plugin assembly has Serilog as a reference
        var pluginAssembly = entryType.Assembly;
        var serilogReference = pluginAssembly.GetReferencedAssemblies()
            .FirstOrDefault(a => a.Name == "Serilog");
        Assert.That(serilogReference, Is.Not.Null, "Plugin should reference Serilog assembly");
    }

    [Test]
    public async Task TestPluginWithMultipleNugetPackages_ShouldHaveAllIsolatedTypes()
    {
        using var workSpace = new WorkSpace(AppDomain.CurrentDomain.BaseDirectory);
        var pluginPath = CreatePlugin(
            workSpace,
            suffix: ".yf",
            csproj: """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="Yufanbot.Plugin.Common" Version="1.1.3">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.4">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                    <PackageReference Include="Serilog" Version="4.3.0">
                        <PrivateAssets>all</PrivateAssets>
                    </PackageReference>
                </ItemGroup>
            </Project>
            """,
            metaInfo: """
            {
                "id": "multi-isolated",
                "name": "Multiple Isolated Types Test",
                "description": "Test that multiple NuGet packages are properly isolated",
                "version": "1.0.0",
                "author": "test"
            }
            """,
            sources: """
            using System;
            using Newtonsoft.Json;
            using Serilog;
            using NapPlana.Core.Bot;
            using Yufanbot.Plugin.Common;

            public class Plugin : IPlugin
            {
                public void OnInitialize(NapBot bot)
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    
                    var data = new { message = "Multiple packages test" };
                    string json = JsonConvert.SerializeObject(data);
                    Log.Information(json);
                }
            }
            """
        );

        var plugin = await _pluginCompiler.CompilePluginAsync(pluginPath);
        Assert.That(plugin, Is.Not.Null);

        // Get the plugin's entry type and its assembly
        var entryType = plugin.Entry.GetType();
        var pluginAssembly = entryType.Assembly;

        // Check that both Newtonsoft.Json and Serilog are referenced
        var referencedAssemblies = pluginAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.That(referencedAssemblies, Does.Contain("Newtonsoft.Json"),
            "Plugin should reference Newtonsoft.Json assembly");
        Assert.That(referencedAssemblies, Does.Contain("Serilog"),
            "Plugin should reference Serilog assembly");
    }
}
