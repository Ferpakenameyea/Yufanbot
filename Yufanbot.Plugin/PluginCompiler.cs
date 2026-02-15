using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using NapPlana.Core.Bot;
using Yufanbot.Config;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Plugin;

public sealed class PluginCompiler : IPluginCompiler
{
    private readonly string cacheRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".plugincache");
    private readonly ILogger<PluginCompiler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PluginCompilerConfig _config;

    public PluginCompiler(
        ILogger<PluginCompiler> logger,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = _serviceProvider.GetRequiredService<IConfigProvider>().Resolve<PluginCompilerConfig>();

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

    public async Task<YFPlugin?> CompilePluginAsync(string path)
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
        string[] cleanTargets = [
            Path.Combine(workSpace.DirectoryInfo.FullName, "bin"),
            Path.Combine(workSpace.DirectoryInfo.FullName, "obj")
        ];
        foreach (var cleanTarget in cleanTargets)
        {
            if (Directory.Exists(cleanTarget))
            {
                Directory.Delete(cleanTarget, recursive: true);
            }
        }
        PluginMeta? meta = GetMeta(workSpace);

        if (meta == null)
        {
            _logger.LogError("Plugin META_INF not found for {name}, skipping loading.", fileInfo.Name);
            return null;
        }

        if (string.IsNullOrWhiteSpace(meta.Id))
        {
            _logger.LogError("Plugin {name} meta is incomplete or invalid (missing required field 'id' or it is blank.)", fileInfo.Name);
            return null;
        }

        (var success, var assembly) = await Compile(workSpace, meta, fileInfo);
        if (!success || assembly == null)
        {
            _logger.LogError("Failed to load {pluginname}.", fileInfo.Name);
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
                    "Plugin {name}(file: {filename}) doesn't have an entry. (An entry is a class that implements IPlugin)",
                        meta.Id,
                        fileInfo.Name);
                return null;
            }

            IPlugin instance = (ActivatorUtilities.CreateInstance(_serviceProvider, entry) as IPlugin)!;

            return new(Entry: instance, Meta: meta);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(
                e, "Found more than one entry in plugin {name}(file: {filename}). Please ensure there is only one entry.",
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

    private async Task<(bool success, Assembly? assembly)> Compile(WorkSpace workSpace, PluginMeta meta, FileInfo pluginFileInfo)
    {
        FileInfo? csprojFile;
        try
        {
            csprojFile = workSpace.DirectoryInfo.GetFiles().Single(f => f.Extension == ".csproj");
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("Found none or multiple .csproj file in when compiling {name}({id}) (plugin at path {filepath})",
                meta.Name,
                meta.Id,
                pluginFileInfo.FullName);
            return (false, null);
        }
        
        var compileTuple = await CSharpLanguage.BuildDllAsync(csprojFile.FullName, _logger);
        if (compileTuple == null)
        {
            _logger.LogError("Failed to compile dll artifact.");
            return (false, null);
        }

        (var rootPath, var entryName) = compileTuple.Value;

        var context = new PluginLoadContext(rootPath, entryName);
        var mainDllPath = Path.Combine(rootPath, entryName);

        Assembly assembly = context.LoadEntryAssembly(mainDllPath);

        return (true, assembly);
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
            // NOTE: Actually this is not necessary for actual run but this will let tests pass, so why not?
            .Append(typeof(NapBot).Assembly.Location)
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