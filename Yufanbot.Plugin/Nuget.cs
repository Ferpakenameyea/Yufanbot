using System.Collections.ObjectModel;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

internal static class Nuget
{
    internal static (string name, string version)? ParsePackageString(string packageString)
    {
        if (string.IsNullOrWhiteSpace(packageString))
        {
            return null;
        }
        
        string[] parts = packageString.Split(':');
        
        switch (parts.Length)
        {
            case 2: 
                var tuple = (parts[0].Trim(), parts[1].Trim());
                if (string.IsNullOrWhiteSpace(tuple.Item1) || 
                    string.IsNullOrWhiteSpace(tuple.Item2))
                {
                    return null;
                }
                return tuple;
            case 1: 
                return (parts[0].Trim(), "latest");
            default: 
                return null;
        };
    }

    public async static Task<NugetResult<string[]>> DownloadPackageFromSources(
        string packageString, 
        ReadOnlyCollection<SourceRepository> repositories)
    {
        var tuple = ParsePackageString(packageString);
        if (tuple == null)
        {
            return new(NugetResolveStatus.InvalidString);
        }
        (var name, var nugetVersionString) = tuple.Value;
        using var cache = new SourceCacheContext();

        NuGetVersion? version = null;
        if (nugetVersionString != "latest")
        {
            try
            {
                version = new(nugetVersionString);
            }
            catch (ArgumentException)
            {
                return new(NugetResolveStatus.InvalidVersion);
            }
        }

        foreach (var repo in repositories)
        {
            if (version == null)
            {
                var metadataResource = await repo.GetResourceAsync<MetadataResource>();
                var versions = await metadataResource.GetVersions(name, cache, NullLogger.Instance, CancellationToken.None);
                
                if (!versions.Any())
                {
                    continue;
                }

                version = versions.Max()!;
            }
            
            var resource = await repo.GetResourceAsync<FindPackageByIdResource>();
            using var ms = new MemoryStream();

            bool found = await resource.CopyNupkgToStreamAsync(
                name,
                version,
                ms,
                cache,
                NullLogger.Instance,
                CancellationToken.None
            );

            if (!found)
            {
                continue;
            }
            
            string packagesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            string packageFolder = Path.Combine(packagesFolder, name.ToLower(), version.ToString());
            Directory.CreateDirectory(packageFolder);

            string nupkgPath = Path.Combine(packageFolder, $"{name}.{nugetVersionString}.nupkg");
            File.WriteAllBytes(nupkgPath, ms.ToArray());

            System.IO.Compression.ZipFile.ExtractToDirectory(nupkgPath, packageFolder, overwriteFiles: true);
            string? frameworkVersion = SelectFramework(new(packageFolder));
            
            if (frameworkVersion == null)
            {
                continue;
            }

            string libPath = Path.Combine(packageFolder, "lib", frameworkVersion);
            
            if (Directory.Exists(libPath))
            {
                string[] files = Directory.GetFiles(libPath, "*.dll");
                return new(files);
            }
        }

        return new(NugetResolveStatus.NotFound);
    }

    private static string? SelectFramework(DirectoryInfo packageDirectory)
    {
        string libRoot = Path.Combine(packageDirectory.FullName, "lib");
        var frameworkDirs = Directory.GetDirectories(libRoot)
                                    .Select(d => Path.GetFileName(d))
                                    .OrderByDescending(f =>
                                    {
                                        if (f.StartsWith("net9")) return 9;
                                        if (f.StartsWith("net8")) return 8;
                                        if (f.StartsWith("net7")) return 7;
                                        if (f.StartsWith("net6")) return 6;
                                        if (f.StartsWith("net5")) return 5;
                                        if (f.StartsWith("netcoreapp3")) return 3;
                                        if (f.StartsWith("netstandard2")) return 2;
                                        return 0;
                                    });

        return frameworkDirs.FirstOrDefault();
    }
}