namespace Yufanbot.Config;

public interface IFileReader
{
    public string? ReadAllText(FileInfo fileInfo);
}