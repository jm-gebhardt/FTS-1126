namespace Gebhardt.Acc.IntegrationTest;

/// <summary>
/// CompleteOrderTest
/// </summary>
[TestClass]
public class CompleteOrderTest
{
    private TestFixture _testFixture = null!;

    /// <summary>
    /// Start all docker services before each test
    /// </summary>
    [TestInitialize]
    public async Task StartServicesAsync()
    {
        _testFixture = new TestFixture();
        await _testFixture.StartAsync();
    }

    /// <summary>
    /// Remove all docker services after test
    /// </summary>
    [TestCleanup]
    public async Task StopServicesAsync()
    {
        await _testFixture.StopAsync();
    }

    /// <summary>
    /// Add one agv and process one order 
    /// </summary>
    [TestMethod]
    public async Task SingleAgvAdd_OrderProcessedSuccessful()
    {
        var agv = await _testFixture.AddAgv("Agv#1");
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agv.agvCorrelationId);
    }

    /// <summary>
    /// Add two agvs and process four orders
    /// </summary>
    [TestMethod]
    public async Task TwoAgvAdd_FourOrderProcessedSuccessful()
    {
        var agv0 = await _testFixture.AddAgv("Agv#1");
        var agv1 = await _testFixture.AddAgv("Agv#2");
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agv0.agvCorrelationId);
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agv1.agvCorrelationId);
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agv0.agvCorrelationId);
        await _testFixture.AddAndCompleteOrder("PS01", "APL1", agv1.agvCorrelationId);
    }
}