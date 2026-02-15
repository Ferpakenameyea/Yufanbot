using System.Diagnostics;
using Microsoft.Extensions.Logging;

internal static class CSharpLanguage
{
    public static async Task<(string rootPath, string entryDllName)?> BuildDllAsync<T>(string csprojPath, ILogger<T> logger)
    {
        FileInfo csprojFile = new(csprojPath);
        
        if (!csprojFile.Exists)
        {
            throw new FileNotFoundException("csproject file doesn't exist!", csprojPath);
        }
        
        var entryDllName = csprojFile.Name[..(csprojFile.Name.Length - csprojFile.Extension.Length)] + ".dll";

        string projectDir = Path.GetDirectoryName(csprojPath)!;

        var psi = new ProcessStartInfo("dotnet", $"publish \"{csprojPath}\" -c Release")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectDir
        };

        var process = Process.Start(psi) ?? throw new Exception("dotnet publish cannot be launched");
        
        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger.LogError("failed to publish dlls: compilation failed");
            logger.LogError("{message}", stdout);
            return null;
        }

        var directories = Directory.GetDirectories(Path.Combine(projectDir, "bin", "Release"));

        try
        {
            var root = Path.Combine(directories[0], "publish");
            return (root, entryDllName);
        }
        catch (InvalidOperationException)
        {
            logger.LogError("Multiple dll files found after compilation.");
            return null;
        }
    }
}