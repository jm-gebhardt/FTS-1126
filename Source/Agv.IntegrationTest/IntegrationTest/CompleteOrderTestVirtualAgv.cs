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

    private RestApi _restApi = null!; 
    
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
        _restApi.ShouldBeNull();
        _dockerComposeVirtualAgv.ShouldBeNull();

        _restApi = new RestApi(
            Environment.GetEnvironmentVariable(EnvVars.KeycloakClientId) ?? string.Empty,
            Environment.GetEnvironmentVariable(EnvVars.KeycloakUsername)?? string.Empty,
            Environment.GetEnvironmentVariable(EnvVars.KeycloakUserPassword)?? string.Empty
        );
        _restApi.ShouldNotBeNull();
        
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
        _dockerComposeVirtualAgv.Up("taragv1").ShouldBeTrue();
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
        const string agvSerialNumber = "agv00";
        const string orderId = "Order#1";
        const string leNo = "123";
        const string source = "WE1";
        const string destination = "APL1";
        
        var restAgv = await _restApi.WaitForAgv(agvSerialNumber);
        restAgv.ShouldNotBeNull();

        var agvCorrelationId = restAgv["correlationId"]?.ToString();
        agvCorrelationId.ShouldNotBeNullOrEmpty();

        (await _restApi.WaitForAgvData(agvCorrelationId)).ShouldNotBeNull();
        (await _restApi.WaitForAgvGraph(agvCorrelationId)).ShouldNotBeNull();
        var restMaps = await _restApi.WaitForMaps();
        var restMap = restMaps!.FirstOrDefault();
        restMap.ShouldNotBeNull();
        (await _restApi.WaitForMap(restMap["correlationId"]?.ToString()!, restAgv["correlationId"]?.ToString()!)).ShouldNotBeNull();


        (await _restApi.SetAgvIdle(agvCorrelationId)).ShouldBeTrue();

        var context = new WcsDbContext();

        (await context.Database.CanConnectAsync()).ShouldBeTrue();

        await context.AddNewOrder(orderId, leNo, source, destination);
        await context.SaveChangesAsync();

        var actionGroup = await _restApi.WaitForFinishedActionGroupForOrder(orderId);
        actionGroup.ShouldNotBeNull();

        context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
    }
}