namespace Yufanbot.Config;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ConfigEntryAttribute(string path, ConfigEntryGetType entryType, bool optional = true) : Attribute
{
    public string Path { get; } = path;
    public ConfigEntryGetType EntryType { get; } = entryType;
    public bool Optional { get; } = optional;
}