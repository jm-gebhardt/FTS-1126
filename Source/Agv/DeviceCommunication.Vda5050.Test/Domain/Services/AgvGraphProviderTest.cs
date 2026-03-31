using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class AgvGraphProviderTest
{
    [TestMethod]
    public async Task GetAgvGraphAsync_LayoutsExist_ReturnsGraph()
    {
        var agvId = Guid.NewGuid();
        var agv = new Agv(agvId, "Maker", "S1", "serial1");
        var layout = GraphLayoutGenerator.GenerateLayout(
            nodes: [GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: agv.VehicleTypeId)]);

        var fixture = new AgvGraphProviderTestFixture()
            .SetAgv(agv)
            .SetLayouts(agv.VehicleTypeId, [layout]);

        var result = await fixture.GraphProvider.GetAgvGraphAsync(agvId);

        result.ShouldNotBeNull();
        result.AgvCorrelationId.ShouldBe(agvId);
        result.Nodes.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetAgvGraphAsync_NoLayouts_ReturnsNull()
    {
        var agvId = Guid.NewGuid();
        var agv = new Agv(agvId, "Maker", "S1", "serial1");

        var fixture = new AgvGraphProviderTestFixture()
            .SetAgv(agv)
            .SetLayouts(agv.VehicleTypeId, []);

        var result = await fixture.GraphProvider.GetAgvGraphAsync(agvId);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetRequiredAgvGraphAsync_NoLayouts_ThrowsAgvGraphNotFoundException()
    {
        var agvId = Guid.NewGuid();
        var agv = new Agv(agvId, "Maker", "S1", "serial1");

        var fixture = new AgvGraphProviderTestFixture()
            .SetAgv(agv)
            .SetLayouts(agv.VehicleTypeId, []);

        await Should.ThrowAsync<AgvGraphNotFoundException>(() => fixture.GraphProvider.GetRequiredAgvGraphAsync(agvId));
    }

    [TestMethod]
    public async Task GetRequiredAgvGraphAsync_LayoutsExist_ReturnsGraph()
    {
        var agvId = Guid.NewGuid();
        var agv = new Agv(agvId, "Maker", "S1", "serial1");
        var layout = GraphLayoutGenerator.GenerateLayout(
            nodes: [GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: agv.VehicleTypeId)]);

        var fixture = new AgvGraphProviderTestFixture()
            .SetAgv(agv)
            .SetLayouts(agv.VehicleTypeId, [layout]);

        var result = await fixture.GraphProvider.GetRequiredAgvGraphAsync(agvId);

        result.ShouldNotBeNull();
        result.AgvCorrelationId.ShouldBe(agvId);
    }
}