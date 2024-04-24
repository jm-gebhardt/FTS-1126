using Gebhardt.Acc.IntegrationTest.Agv;
using Gebhardt.Acc.IntegrationTest.Utils;
using Gebhardt.Acc.IntegrationTest.Utils.Db;
using Gebhardt.Acc.IntegrationTest.Utils.Db.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;


namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// CompleteOrderTest
/// </summary>
// /////// Graph for integration test ///////////////////////////////
// 
// Pick: SRC_01
// Drop: DST_01, DST_02
// Pick/Drop: STN_01
// Charge: CRG_01
// Park: -
// 
//            SRC_01                         STN_01         0     //
//             |                               |            |     //
//            11 -------- 21                  41           -1     //
//                         |                   |            |     //
//            12 <<<<<<<< 22                  42           -2     //
//             V           |                   |            |     //
// CRG_01 --- 13 >>>>>>>> 23 ------- 33 ----- 43           -3     //
//                                    |        |            |     //
//                                 DST_01     DST_02       -4     //
//                                                          |     //
//   0  ---    1   ---    2   ---    3   ---    4   ---     5   - //
// /////////////////////////////////////////////////////////////////

[TestClass]
public class CompleteOrderTest
{
    private DockerCompose? _dockerCompose;

    /// <summary>
    /// Start all docker services before each test
    /// </summary>
    [TestInitialize]
    public void StartServices()
    {
        _dockerCompose.ShouldBeNull();
        _dockerCompose = new DockerCompose(EnvVars.ComposeFileEnvVar);
        _dockerCompose.AreRemoved().ShouldBeTrue();
        _dockerCompose.Up().ShouldBeTrue();
        _dockerCompose.AreRunning().ShouldBeTrue();
    }

    /// <summary>
    /// Remove all docker services after test
    /// </summary>
    [TestCleanup]
    public void StopServices()
    {
        _dockerCompose.ShouldNotBeNull();
        _dockerCompose.Down().ShouldBeTrue();
        _dockerCompose.AreRemoved().ShouldBeTrue();
        _dockerCompose = null;
    }

    /// <summary>
    /// Add one agv and process one order 
    /// </summary>
    [TestMethod]
    public async Task SingleAgvAdd_OrderProcessedSuccessful()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var agv = await AddAgv("Agv#1", cancellationTokenSource.Token);
        _ = agv.SubscribeToCompleteOrdersSuccessfully();
        await AddAndCompleteOrder("Order#1");

        await cancellationTokenSource.CancelAsync();
        cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Add two agvs and process four orders
    /// </summary>
    [TestMethod]
    public async Task TwoAgvAdd_FourOrderProcessedSuccessful()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var agv0 = await AddAgv("Agv#1", cancellationTokenSource.Token);
        var agv1 = await AddAgv("Agv#2", cancellationTokenSource.Token);
        _ = agv0.SubscribeToCompleteOrdersSuccessfully();
        _ = agv1.SubscribeToCompleteOrdersSuccessfully();
        await AddAndCompleteOrder("Order#1");
        await AddAndCompleteOrder("Order#2");
        await AddAndCompleteOrder("Order#3");
        await AddAndCompleteOrder("Order#4");

        await cancellationTokenSource.CancelAsync();
        cancellationTokenSource.Dispose();
    }

    private async Task<TestAgv> AddAgv(string agvSerialNumber, CancellationToken token)
    {
        var restApi = new RestApi();

        var agv = new TestAgv(agvSerialNumber);
        await agv.SubscribeDataTopics();

        _ = agv.StartSendingStatus(token);

        var restAgv = await restApi.WaitForAgv(agv.SerialNumber);
        restAgv.ShouldNotBeNull();

        (await restApi.WaitForAgvData(restAgv["correlationId"]?.ToString())).ShouldNotBeNull();
        (await restApi.WaitForAgvGraph(restAgv["correlationId"]?.ToString())).ShouldNotBeNull();
        var restMaps = await restApi.WaitForMaps();
        var restMap = restMaps!.FirstOrDefault();
        restMap.ShouldNotBeNull();
        (await restApi.WaitForMap(restMap["correlationId"]?.ToString()!, restAgv["correlationId"]?.ToString()!)).ShouldNotBeNull();

        (await restApi.SetAgvIdle(restAgv["correlationId"]?.ToString())).ShouldBeTrue();

        return agv;
    }

    private async Task AddAndCompleteOrder(string orderId)
    {
        const string leNo = "123";
        const string source = "SRC_01";
        const string destination = "DST_01";
        var context = new WcsDbContext();

        (await context.Database.CanConnectAsync()).ShouldBeTrue();

        await context.AddNewOrder(orderId, leNo, source, destination);
        await context.SaveChangesAsync();

        var restApi = new RestApi();
        var actionGroup = await restApi.WaitForFinishedActionGroupForOrder(orderId);
        actionGroup.ShouldNotBeNull();
        
        context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
    }
}