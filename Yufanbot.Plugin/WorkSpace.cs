namespace Yufanbot.Plugin;

public readonly struct WorkSpace : IDisposable
{
    public DirectoryInfo DirectoryInfo { get; }
    public Guid Guid { get; } = Guid.NewGuid();

    public WorkSpace(string root)
    {
        DirectoryInfo = new DirectoryInfo(Path.Combine(root, Guid.ToString()));
        DirectoryInfo.Create();
    }

    public readonly void Dispose()
    {
        DirectoryInfo.Delete(recursive: true);
    }
}