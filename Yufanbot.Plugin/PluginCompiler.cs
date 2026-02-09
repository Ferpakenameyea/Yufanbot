using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using Yufanbot.Config;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Plugin;

public sealed class PluginCompiler : IPluginCompiler
{
    private readonly string cacheRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".plugincache");
    private readonly ILogger<PluginCompiler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PluginCompilerConfig _config;
    private readonly ReadOnlyCollection<SourceRepository> _nugetRepositories;

    public PluginCompiler(
        ILogger<PluginCompiler> logger,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = _serviceProvider.GetRequiredService<IConfigProvider>().Resolve<PluginCompilerConfig>();
        var providers = Repository.Provider.GetCoreV3();
        _nugetRepositories = _config.NugetSources
            .Select(s => new SourceRepository(new PackageSource(s), providers))
            .ToList()
            .AsReadOnly();

        var cacheDirectory = new DirectoryInfo(cacheRoot);
        if (!cacheDirectory.Exists)
        {
            try
            {
                cacheDirectory.Create();
            }
            catch (IOException e)
            {
                _logger.LogCritical(e, "IOException when trying to create cache directory.");
                throw;
            }
            return;
        }

        foreach (var f in cacheDirectory.GetFiles())
        {
            // clean cache
            try
            {
                f.Delete();
            }
            catch (Exception e)
            {
                logger.LogWarning("Exception when trying to delete {filename} in compiler cache: {message}", f.Name, e.Message);
            }
        }
    }

    public async Task<Common.YFPlugin?> CompilePluginAsync(string path)
    {
        FileInfo fileInfo = new(path);

        if (!fileInfo.Exists)
        {
            _logger.LogError("Given plugin path {path} doesn't exist!", path);
            return null;
        }

        var suffixSeparatorIndex = fileInfo.Name.LastIndexOf('.');
        if (suffixSeparatorIndex == -1 ||
            fileInfo.Name[suffixSeparatorIndex..] != IPlugin.PluginSuffix)
        {
            _logger.LogError("Given file {name} is not a bot plugin, bot plugin needs to end with {suffix}",
                fileInfo.Name,
                IPlugin.PluginSuffix);
            return null;
        }

        _logger.LogInformation("Loading plugin {name}.", fileInfo.Name);

        using var workSpace = new WorkSpace(cacheRoot);

        try
        {
            ZipFile.ExtractToDirectory(
                path,
                workSpace.DirectoryInfo.FullName
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error extracting plugin at {path}", path);
            return null;
        }

        PluginMeta? meta = GetMeta(workSpace);

        if (meta == null)
        {
            _logger.LogError("Plugin META_INF not found for {name}, skipping loading.", fileInfo.Name);
            return null;

        }
        NugetResult<string[]> dependenciesResolveResult = await ResolveDependencies(meta);
        if (!dependenciesResolveResult.Success)
        {
            _logger.LogError("Failed to resolve nuget packages for {id}({path})",
                meta.Id,
                fileInfo.FullName);
            return null;
        }
        string[] dependenciesPaths = dependenciesResolveResult.Value!;

        if (!Compile(workSpace, meta, fileInfo, dependenciesPaths, out Assembly? assembly))
        {
            _logger.LogError("Failed to load {}.", fileInfo.Name);
            return null;
        }


        Type? entry = null;
        try
        {
            entry = assembly.GetTypes()
                .SingleOrDefault(type => type.IsAssignableTo(typeof(IPlugin)));
            if (entry == null)
            {
                _logger.LogError(
                    "Plugin {name}(file: {filename}) doesn't have an entry. (An entry is a class thta implements IPlugin)",
                        meta.Id,
                        fileInfo.Name);
                return null;
            }

            IPlugin instance = (ActivatorUtilities.CreateInstance(_serviceProvider, entry) as IPlugin)!;

            return new(Entry: instance, Meta: meta);
        }
        catch (InvalidOperationException)
        {
            _logger.LogError(
                "Found more than one entry in plugin {name}(file: {filename}). Please ensure there is only one entry.",
                meta.Id,
                fileInfo.Name
            );
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Failed when initializing plugin {name}(file: {filename}) entry: {typefullname}.",
                meta.Id,
                fileInfo.Name,
                entry?.Name ?? "<Entry Not Loaded>");
            return null;
        }
    }

    private async Task<NugetResult<string[]>> ResolveDependencies(PluginMeta meta)
    {
        if (meta.NugetDependencies.Count == 0)
        {
            return new([]);
        }

        List<string> list = [];
        foreach (var dependency in meta.NugetDependencies)
        {
            var result = await Nuget.DownloadPackageFromSources(dependency,
                _nugetRepositories);

            if (!result.Success)
            {
                _logger.LogError("Failed to resolve dependency for {id} ({dependency}) ({reason})",
                    meta.Id,
                    dependency,
                    result.Status);
                return new(result.Status);
            }

            list.AddRange(result.Value!);
        }

        return new([.. list]);
    }

    private bool Compile(WorkSpace workSpace, PluginMeta meta, FileInfo pluginFileInfo, string[] nugetDependenciesPaths, [NotNullWhen(true)] out Assembly? assembly)
    {
        var sources = workSpace.DirectoryInfo.GetFiles("*.cs", SearchOption.AllDirectories);
        var syntaxTrees = sources.Select(file =>
            CSharpSyntaxTree.ParseText(
                File.ReadAllText(file.FullName),
                path: file.FullName
            )
        );

        var references = GetAllReferences(nugetDependenciesPaths);

        var compilation = CSharpCompilation.Create(
            assemblyName: meta.Id,
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            _logger.LogError("Failed to load {name}: Compilation error", pluginFileInfo.Name);
            foreach (var e in errors)
            {
                _logger.LogError("{errorMessage}", e.GetMessage());
            }
            assembly = null;
            return false;
        }

        ms.Seek(0, SeekOrigin.Begin);
        assembly = Assembly.Load(ms.ToArray());
        return true;
    }

    private PluginMeta? GetMeta(WorkSpace workSpace)
    {
        string metaPath = Path.Combine(workSpace.DirectoryInfo.FullName, "META_INF");
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            var text = File.ReadAllText(metaPath);
            return JsonSerializer.Deserialize<PluginMeta>(text);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occured when trying to load meta");
            return null;
        }
    }

    private static List<MetadataReference> GetAllReferences(IEnumerable<string> nugetDlls)
    {
        var references = new List<MetadataReference>();

        var loadedAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && IsManagedAssembly(a.Location))
            .Select(a => a.Location)
            .ToList();

        references.AddRange(loadedAssemblies.Select(path => MetadataReference.CreateFromFile(path)));

        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var bclDlls = Directory.EnumerateFiles(runtimeDir, "*.dll")
            .Where(f => !loadedAssemblies.Contains(f, StringComparer.OrdinalIgnoreCase) && IsManagedAssembly(f));

        references.AddRange(bclDlls.Select(path => MetadataReference.CreateFromFile(path)));

        if (nugetDlls != null)
        {
            references.AddRange(nugetDlls.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)));
        }

        references = references
            .GroupBy(r => r.Display, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        return references;
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var peReader = new PEReader(stream);
            return peReader.HasMetadata;
        }
        catch
        {
            return false;
        }
    }
}