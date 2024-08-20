using Gebhardt.Acc.IntegrationTest.Utils;
using Gebhardt.Acc.IntegrationTest.Utils.Db;
using Gebhardt.Acc.IntegrationTest.Utils.Db.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;


namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// CompleteOrderTest
/// </summary>
[TestClass]
public class CompleteOrderTestVirtualAgv
{
    private DockerCompose? _dockerCompose;
    private DockerCompose? _dockerComposeVirtualAgv;

    private const string DockerRegistry = "registry.gitlab.com";
    private const string DockerRegistryRepository = "registry.gitlab.com/tar4/karis_virtual";
    private static readonly string[] ExcludedContainers = new[] { "mssqlbootstrap_1" };

    private const string DockerRegistryUser = "gebhardt";

    private readonly string _dockerRegistryPassword =
        Environment.GetEnvironmentVariable(EnvVars.DockerRegistryPassword)
        ?? throw new InvalidOperationException($"Environment variable {EnvVars.DockerRegistryPassword} must be set.");

    /// <summary>
    /// Start all docker services before each test
    /// </summary>
    [TestInitialize]
    public void StartServices()
    {
        _dockerCompose.ShouldBeNull();
        _dockerComposeVirtualAgv.ShouldBeNull();

        _dockerCompose = new DockerCompose(EnvVars.ComposeFileEnvVar);
        _dockerComposeVirtualAgv = new DockerCompose(EnvVars.ComposeFileEnvVarVirtualAgv, DockerRegistry,
            DockerRegistryRepository, DockerRegistryUser, _dockerRegistryPassword);
        _dockerCompose.AreRemoved().ShouldBeTrue();
        _dockerComposeVirtualAgv.AreRemoved().ShouldBeTrue();

        //Start Leitsteuerung
        _dockerCompose.Up().ShouldBeTrue();
        _dockerCompose.AreRunning(ExcludedContainers).ShouldBeTrue();

        //Start Virtual AGV
        _dockerComposeVirtualAgv.Login().ShouldBeTrue();
        _dockerComposeVirtualAgv.Pull().ShouldBeTrue();
        _dockerComposeVirtualAgv.Up("karis0 mongo-provisioning").ShouldBeTrue();
        _dockerComposeVirtualAgv.AreRunning(ExcludedContainers).ShouldBeTrue();
    }

    /// <summary>
    /// Remove all docker services after test
    /// </summary>
    [TestCleanup]
    public void StopServices()
    {
        _dockerComposeVirtualAgv.ShouldNotBeNull();
        _dockerComposeVirtualAgv.Down().ShouldBeTrue();
        _dockerComposeVirtualAgv.AreRemoved().ShouldBeTrue();
        _dockerCompose.ShouldNotBeNull();
        _dockerCompose.Down().ShouldBeTrue();
        _dockerCompose.AreRemoved().ShouldBeTrue();
        _dockerCompose = null;
        _dockerComposeVirtualAgv = null;
    }

    /// <summary>
    /// Add one agv and process one order 
    /// </summary>
    [TestMethod]
    public async Task SingleVirtualAgvAdd_OrderProcessedSuccessful()
    {
        const string agvSerialNumber = "karis0";
        const string orderId = "Order#1";
        const string leNo = "123";
        const string source = "PVB02";
        const string destination = "PVB03";

        var restApi = new RestApi();
        var restAgv = await restApi.WaitForAgv(agvSerialNumber);
        restAgv.ShouldNotBeNull();

        var agvCorrelationId = restAgv["correlationId"]?.ToString();
        agvCorrelationId.ShouldNotBeNullOrEmpty();

        (await restApi.WaitForAgvData(agvCorrelationId)).ShouldNotBeNull();
        (await restApi.WaitForAgvGraph(agvCorrelationId)).ShouldNotBeNull();
        var restMaps = await restApi.WaitForMaps();
        var restMap = restMaps!.FirstOrDefault();
        restMap.ShouldNotBeNull();
        (await restApi.WaitForMap(restMap["correlationId"]?.ToString()!, restAgv["correlationId"]?.ToString()!)).ShouldNotBeNull();


        (await restApi.SetAgvIdle(agvCorrelationId)).ShouldBeTrue();

        var context = new WcsDbContext();

        (await context.Database.CanConnectAsync()).ShouldBeTrue();

        await context.AddNewOrder(orderId, leNo, source, destination);
        await context.SaveChangesAsync();

        var actionGroup = await restApi.WaitForFinishedActionGroupForOrder(orderId);
        actionGroup.ShouldNotBeNull();

        context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
    }
}