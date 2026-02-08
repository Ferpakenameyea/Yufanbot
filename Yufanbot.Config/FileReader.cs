using Yufanbot.Config;

namespace Yufanbot.Config;

public sealed class FileReader : IFileReader
{
    public string? ReadAllText(FileInfo fileInfo)
    {
        DirectoryInfo? directory = fileInfo.Directory;
        if (directory != null && !directory.Exists)
        {
            directory.Create();
        }
        if (!fileInfo.Exists)
        {
            fileInfo.Create();
            return string.Empty;
        }

        return File.ReadAllText(fileInfo.FullName);
    }
}