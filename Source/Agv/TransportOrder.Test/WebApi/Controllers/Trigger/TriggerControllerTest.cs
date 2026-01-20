using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

[TestClass]
public class TriggerControllerTest
{
    [TestMethod]
    public async Task GetTrigger_ValidData_ReturnsTriggerDto()
    {
        var trigger = TriggerGenerator.CreateTrigger();
        var fixture = new TriggerControllerTestFixture()
            .WithTrigger(trigger)
            .Build();

        var triggerDto = await fixture.GetControllerResultValueAsync(c => c.GetTrigger(trigger.Id));

        triggerDto.Name.ShouldBe(trigger.DisplayName);
        triggerDto.Id.ShouldBe(trigger.Id);
    }

    [TestMethod]
    public async Task AssignTriggerToNodeLocation_ValidData_ReturnsLocationDto()
    {
        var nodeToUse = "WP-0001".ToGraphNode();
        var location = new TriggerLocationDto
        {
            NodeId = nodeToUse.Name
        };

        var fixture = new TriggerControllerTestFixture()
            .WithTriggerLocation(location, nodeToUse)
            .Build();

        var locationDto = await fixture.GetControllerResultAsync(c => c.AssignTriggerToLocation(Guid.NewGuid(), location));
        locationDto.Id.ShouldBe(location.Id);
        locationDto.NodeId.ShouldBe(nodeToUse.Name);
    }

    [TestMethod]
    public async Task AssignTriggerToPolygonLocation_ValidData_ReturnsLocationDto()
    {
        var location = new TriggerLocationDto
        {
            Polygon = new List<PolygonPointDto>
            {
                { new(1.0d, 1.0d) },
                { new(1.0d, 2.0d) },
                { new(2.0d, 1.0d) }
            }
        };

        var fixture = new TriggerControllerTestFixture()
            .WithTriggerLocation(location, "node".ToGraphNode())
            .Build();

        var locationDto = await fixture.GetControllerResultAsync(c => c.AssignTriggerToLocation(Guid.NewGuid(), location));
        locationDto.Id.ShouldBe(location.Id);
        locationDto.Polygon?.Count.ShouldBe(location.Polygon.Count);
    }

    [TestMethod]
    public async Task UpdateLocationConfigurationParameter_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.UpdateLocationConfigurationParameter(Guid.NewGuid(), Guid.NewGuid(), "name", "new value"));
    }

    [TestMethod]
    public async Task ResetLocationConfigurationParameter_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.ResetLocationConfigurationParameter(Guid.NewGuid(), Guid.NewGuid(), "name"));
    }

    [TestMethod]
    public async Task RemoveTriggerLocation_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.RemoveTriggerLocation(Guid.NewGuid(), Guid.NewGuid()));
    }

    [TestMethod]
    public async Task UpdateConfigurationParameter_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.UpdateDefaultConfigurationParameter(Guid.NewGuid(), "name", "new value"));
    }

    [TestMethod]
    public async Task DeleteTrigger_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.DeleteTrigger(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task RestoreTrigger_ValidData_Returns()
    {
        var fixture = new TriggerControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.RestoreTrigger(Guid.NewGuid()));
    }
}