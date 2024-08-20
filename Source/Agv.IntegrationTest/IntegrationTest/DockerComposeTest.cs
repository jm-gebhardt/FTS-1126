using Gebhardt.Acc.IntegrationTest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// Starts and stops docker compose service
/// </summary>
[TestClass]
public class DockerComposeTest
{
    private const string DockerRegistry = "registry.gitlab.com";
    private const string DockerRegistryRepository = "registry.gitlab.com/tar4/karis_virtual";
    private static readonly string[] ExcludedContainers = new[] { "mssqlbootstrap_1" };

    private const string DockerRegistryUser = "gebhardt";

    private readonly string _dockerRegistryPassword =
        Environment.GetEnvironmentVariable(EnvVars.DockerRegistryPassword)
        ?? throw new InvalidOperationException($"Environment variable {EnvVars.DockerRegistryPassword} must be set.");

    /// <summary>
    /// Start and stop all services without VirtualAgv
    /// </summary>
    [TestMethod]
    public void ComposeTest()
    {
        var dc = new DockerCompose(EnvVars.ComposeFileEnvVar);
        dc.Up().ShouldBeTrue();
        dc.AreRunning(ExcludedContainers).ShouldBeTrue();
        dc.Down().ShouldBeTrue();
        dc.AreRemoved().ShouldBeTrue();
    }

    /// <summary>
    /// Start and stop all services
    /// </summary>
    [TestMethod]
    public void CombinedComposeTest()
    {
        
        var dc = new DockerCompose(EnvVars.ComposeFileEnvVar);
        var dcVirtual = new DockerCompose(EnvVars.ComposeFileEnvVarVirtualAgv, DockerRegistry,
            DockerRegistryRepository, DockerRegistryUser, _dockerRegistryPassword);
        dc.Up().ShouldBeTrue();
        dc.AreRunning(ExcludedContainers).ShouldBeTrue();
        dcVirtual.Login();
        dcVirtual.Pull();
        dcVirtual.Up("karis0 mongo-provisioning").ShouldBeTrue();
        dcVirtual.AreRunning(ExcludedContainers).ShouldBeTrue();
        dcVirtual.Down().ShouldBeTrue();
        dcVirtual.AreRemoved().ShouldBeTrue();
        dc.Down().ShouldBeTrue();
        dc.AreRemoved().ShouldBeTrue();
    }
}