using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yufanbot.Config;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Plugin;

public sealed class PluginCompiler : IPluginCompiler
{
    private readonly string cacheRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".plugincache");
    private readonly ILogger<PluginCompiler> _logger;
    private readonly IServiceProvider _serviceProvider;
    // NOTE: this config originally provides nuget source list. But is unused now.
    //       Reserved for future use.
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

    private bool EnsurePathValid(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            _logger.LogError("Given plugin path {path} doesn't exist!", fileInfo.FullName);
            return false;
        }

        var suffixSeparatorIndex = fileInfo.Name.LastIndexOf('.');
        if (suffixSeparatorIndex == -1 ||
            fileInfo.Name[suffixSeparatorIndex..] != IPlugin.PluginSuffix)
        {
            _logger.LogError("Given file {name} is not a bot plugin, bot plugin needs to end with {suffix}",
                fileInfo.Name,
                IPlugin.PluginSuffix);
            return false;
        }

        return true;
    }

    public async Task<YFPlugin?> CompilePluginAsync(string path)
    {
        FileInfo fileInfo = new(path);

        if (!EnsurePathValid(fileInfo))
        {
            return null;
        }

        _logger.LogInformation("Loading plugin {name}.", fileInfo.Name);

        using var workSpace = new WorkSpace(cacheRoot);

        if (!TryExtractPlugin(path, workSpace))
        {
            return null;
        }

        PluginMeta? meta = GetMeta(workSpace);

        if (meta == null)
        {
            _logger.LogError("Plugin META_INF not found for {name}, skipping loading.", fileInfo.Name);
            return null;
        }

        if (!IsValidMeta(meta))
        {
            _logger.LogError("Plugin {name} meta is incomplete or invalid.", fileInfo.Name);
            return null;
        }

        var pluginAssembly = await Compile(workSpace, meta, fileInfo);
        if (pluginAssembly == null)
        {
            _logger.LogError("Failed to load {pluginname}.", fileInfo.Name);
            return null;
        }

        return TryCreatePluginEntry(fileInfo, meta, pluginAssembly);
    }

    private static bool IsValidMeta(PluginMeta meta)
    {
        return !string.IsNullOrWhiteSpace(meta.Id);
    }

    private YFPlugin? TryCreatePluginEntry(FileInfo fileInfo, PluginMeta meta, Assembly pluginAssembly)
    {
        Type? entry = null;
        try
        {
            entry = pluginAssembly.GetTypes()
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

    private bool TryExtractPlugin(string path, WorkSpace workSpace)
    {
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
            return false;
        }
        string[] cleanTargets = [
            Path.Combine(workSpace.DirectoryInfo.FullName, "bin"),
            Path.Combine(workSpace.DirectoryInfo.FullName, "obj")
        ];
        foreach (string cleanTarget in cleanTargets)
        {
            if (Directory.Exists(cleanTarget))
            {
                Directory.Delete(cleanTarget, recursive: true);
            }
        }
        return true;
    }

    private async Task<Assembly?> Compile(WorkSpace workSpace, PluginMeta meta, FileInfo pluginFileInfo)
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
            return null;
        }
        
        var compileTuple = await CSharpLanguage.BuildDllAsync(csprojFile.FullName, _logger);
        if (compileTuple == null)
        {
            _logger.LogError("Failed to compile dll artifact.");
            return null;
        }

        (var rootPath, var entryName) = compileTuple.Value;

        var context = new PluginLoadContext(rootPath, entryName);
        var mainDllPath = Path.Combine(rootPath, entryName);

        Assembly assembly = context.LoadEntryAssembly(mainDllPath);

        return assembly;
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
}