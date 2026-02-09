internal static class FileInfoExtensions
{
    public static string? Suffix(this FileInfo file)
    {
        int index = file.Name.LastIndexOf('.');       
        if (index == -1)
        {
            return null;
        }

        return file.Name[index..];
    }
}