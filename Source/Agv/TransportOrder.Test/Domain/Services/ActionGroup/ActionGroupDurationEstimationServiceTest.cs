using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class ActionGroupDurationEstimationServiceTest
{
    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_FirstDeviationEstimateForAgvAndTargetNodes_AddToInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow);
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory:false);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(false);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.value.ShouldBe(deviation.value);
        inMemoryDeviation.lastUpdated.ShouldBeGreaterThan(deviation.lastUpdated);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_DeviationEstimateForAgvAndTargetNodesExists_GetFromInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow);
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory: true);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(true);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.ShouldBe(deviation);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_OutdatedDeviationEstimateForAgvAndTargetNodesExists_UpdateInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow.AddHours(-1));
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory: true);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(true);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.value.ShouldBe(deviation.value);
        inMemoryDeviation.lastUpdated.ShouldBeGreaterThan(deviation.lastUpdated);
    }
}