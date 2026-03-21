namespace Yufanbot.Plugin.Common.Registration;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class ListenToEventAttribute(EventType eventType, int priority = 0) : Attribute
{
    public EventType RegisterEventType { get; } = eventType;
    public int Priority { get; } = priority;
}