namespace Yufanbot.Config;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public sealed class CaseMatchAttribute(CaseMatchMode mode) : Attribute
{
    public CaseMatchMode Mode { get; } = mode;
}