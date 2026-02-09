using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Yufanbot.Plugin.Test;

[TestFixture]
public class NugetDownloadTests
{
    private ReadOnlyCollection<SourceRepository> _repositories;

    [SetUp]
    public void Setup()
    {
        string[] sources = ["https://api.nuget.org/v3/index.json"];
        var providers = Repository.Provider.GetCoreV3();
        _repositories = sources
            .Select(s => new SourceRepository(new PackageSource(s), providers))
            .ToList()
            .AsReadOnly();
    }

    [Test]
    public async Task DownloadNewtonsoftJson_ShouldSuccess()
    {
        var result = await Nuget.DownloadPackageFromSources(
            "Newtonsoft.Json:latest",
            _repositories
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(NugetResolveStatus.Ok));
            Assert.That(result.Value, Is.Not.Empty);
        });
    }

    [Test]
    public async Task DownloadUnknownPackage_ShouldFail()
    {
        var result = await Nuget.DownloadPackageFromSources(
            "Newwwwwwwtonsoft.Json:latest",
            _repositories
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(NugetResolveStatus.NotFound));
        });
    }

    [Test]
    public async Task DownloadWithInvalidVersion_ShouldFail()
    {
        var result = await Nuget.DownloadPackageFromSources(
            "Newtonsoft.Json:lllllatest",
            _repositories
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(NugetResolveStatus.InvalidVersion));
        });
    }
}