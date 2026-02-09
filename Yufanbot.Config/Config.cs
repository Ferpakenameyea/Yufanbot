using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Yufanbot.Config;

public abstract class Config<T> : IConfig where T : Config<T>
{
    private readonly ILogger<T> _logger;
    private readonly IFileReader _fileReader;
    private readonly IEnvironmentVariableProvider _environmentVariableProvider;
    private static readonly string _baseFileName = AppDomain.CurrentDomain.BaseDirectory;
    protected FileInfo ConfigFile => new(Path.Combine(_baseFileName, "config", $"{GetType().Name}_Config.json"));

    public Config(ILogger<T> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider)
    {
        _logger = logger;
        _fileReader = fileReader;
        _environmentVariableProvider = environmentVariableProvider;
        ResolveConfiguration();
    }

    private void ResolveConfiguration()
    {
        var entriesEnumerable = 
                      from p in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                      let att = p.GetCustomAttribute<ConfigEntryAttribute>()
                      where att != null
                      select (Property: p, Attribute: att);

        var entriesList = entriesEnumerable.ToList();
        if (entriesList.Count == 0)
        {
            return;
        }

        string? configJson = null;
        JToken? configJsonObject = null;
        
        try
        {
            configJson = _fileReader.ReadAllText(ConfigFile);
            configJsonObject = configJson != null ? JsonConvert.DeserializeObject<JToken>(configJson) : null;
        } 
        catch (IOException e)
        {
            _logger.LogError(e, "IOException when trying to read from {name}", 
                ConfigFile.Name);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Json parsing error when trying to parse json configuration in {name}", 
                ConfigFile.Name);
        }
        
        if (configJsonObject == null)
        {
            _logger.LogWarning("Nothing present in config file or failed to read it. ({name})", ConfigFile.Name);
        }

        foreach (var entry in entriesList)
        {
            ResolveEntry(entry.Property, entry.Attribute, configJsonObject);
        }

    }

    private void ResolveEntry(PropertyInfo property, ConfigEntryAttribute attribute, JToken? configRoot)
    {
        string? valueString = attribute.EntryType switch 
        {
            ConfigEntryGetType.FromConfigFile => GetFromConfigFile(configRoot, attribute.Path),
            ConfigEntryGetType.FromEnvironment => GetFromEnvironment(attribute.Path, property.PropertyType),
            _ => null
        };

        if (valueString == null)
        {
            if (!attribute.Optional)
            {
                _logger.LogWarning("Required config entry {name} remains to be null after configuration resolving, using default. ({value})", 
                    property.Name,
                    property.GetValue(this));   
            }
            return;
        }

        Type valueType = property.PropertyType;
        try
        {
            object? value;
            if (valueType.IsEnum)
            {
                if (valueString.Length < 2 || valueString[0] != '\"' || valueString[^1] != '\"')
                {
                    _logger.LogError(
                        "Expected a string type (wrapped with \") " + 
                        "to parse to enum ({type}) for property {propertyName}, but given: {actual}",
                        
                        valueType.FullName,
                        property.Name,
                        valueString);
                    return;
                }

                var caseMatchAttribute = property.GetCustomAttribute<CaseMatchAttribute>();
                
                bool ignoreCase = caseMatchAttribute == null || caseMatchAttribute.Mode == CaseMatchMode.IgnoreCase;
                
                if (!Enum.TryParse(valueType, valueString[1..^1], ignoreCase, out value))
                {
                    _logger.LogError(
                        "Failed to parse {string} to enum value type {typename}(ignore case: {ignorecase})",
                        valueString,
                        valueType.FullName,
                        ignoreCase
                    );
                    return;
                }
            }
            else 
            {
                value = JsonConvert.DeserializeObject(valueString, valueType);
            }
            property.SetValue(this, value);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error parsing configuration value");
        }
    }

    private string? GetFromEnvironment(string path, Type propertyType)
    {
        var raw = _environmentVariableProvider.GetEnvironmentVariable(path);
        if (propertyType == typeof(string) || propertyType.IsEnum)
        {
            return JsonConvert.SerializeObject(raw);
        }
        else if (propertyType == typeof(char))
        {
            return JsonConvert.SerializeObject(raw);
        }
        return raw;
    }

    private string? GetFromConfigFile(JToken? configRoot, string path)
    {
        if (configRoot == null)
        {
            _logger.LogWarning("Cannot get config entry {entryname} from config file because it's not present!", path);
            return null;
        }

        string[] paths = path.Split('.');
        JToken? current = configRoot;
        foreach (var p in paths)
        {
            current = current?[p];
        }

        return current?.ToString(Formatting.None);
    }
}