using Gebhardt.Acc.IntegrationTest.Helpers;
using Gebhardt.Acc.IntegrationTest.Utils;
using Shouldly;

namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// Starts and stops docker compose service
/// </summary>
[TestClass]
public class DockerComposeTest
{
    private readonly string _dockerRegistryUrl = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryUrl);
    private readonly string _dockerRegistryRepository = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryRepository);
    private readonly string _dockerRegistryUser = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryUser);
    private readonly string _dockerRegistryPassword = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryPassword);
    private readonly string _nugetPat = EnvHelper.GetRequiredEnvVar(EnvVars.NugetPat);

    /// <summary>
    /// Start and stop all services without VirtualAgv
    /// </summary>
    [TestMethod]
    public async Task ComposeTest()
    {
        var dc = new DockerCompose([EnvVars.ComposeFileEnvVar]);
        await dc.InitializeAsync();
        (await dc.AreRemovedAsync()).ShouldBeTrue();
        (await dc.UpAsync()).ShouldBeTrue();
        (await dc.DownAsync()).ShouldBeTrue();
        (await dc.AreRemovedAsync()).ShouldBeTrue();
    }

    /// <summary>
    /// Start and stop all services
    /// </summary>
    [TestMethod]
    public async Task CombinedComposeTest()
    {
        var dc = new DockerCompose([EnvVars.ComposeFileEnvVar]);
        await dc.InitializeAsync();
        var dcVirtual = new DockerCompose([EnvVars.ComposeFileEnvVarVirtualAgv],
            _dockerRegistryUrl,
            _dockerRegistryRepository,
            _dockerRegistryUser,
            _dockerRegistryPassword,
            _nugetPat,
            dc.EnvFile);
        (await dc.AreRemovedAsync()).ShouldBeTrue();
        (await dc.UpAsync()).ShouldBeTrue();
        (await dcVirtual.LoginAsync()).ShouldBeTrue();
        (await dcVirtual.PullAsync()).ShouldBeTrue();
        (await dcVirtual.AreRemovedAsync()).ShouldBeTrue();
        (await dcVirtual.UpAsync()).ShouldBeTrue();
        (await dcVirtual.DownAsync()).ShouldBeTrue();
        (await dcVirtual.AreRemovedAsync()).ShouldBeTrue();
        (await dc.DownAsync()).ShouldBeTrue();
        (await dc.AreRemovedAsync()).ShouldBeTrue();
    }
}