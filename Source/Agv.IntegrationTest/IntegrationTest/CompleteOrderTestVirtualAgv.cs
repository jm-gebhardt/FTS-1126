namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// CompleteOrderTest
/// </summary>
[TestClass]
public class CompleteOrderTestVirtualAgv
{
    private TestFixture _testFixture = null!;

    /// <summary>
    /// Remove all docker services after test
    /// </summary>
    [TestCleanup]
    public async Task StopServices()
    {
        await _testFixture.StopAsync();
    }

    /// <summary>
    /// Add one agv and process one order 
    /// </summary>
    [TestMethod]
    public async Task SingleVirtualAgvAdd_OrderProcessedSuccessful()
    {
        const string agvSerialNumber = "agv00";
        _testFixture = new TestFixture()
            .WithVirtualAgv();

        await _testFixture.StartAsync();

        var agvCorrelationId = await _testFixture.WaitForAgv(agvSerialNumber);
        await _testFixture.WaitForAgvIdle(agvCorrelationId, TimeSpan.FromMinutes(5));
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agvCorrelationId);
    }
}