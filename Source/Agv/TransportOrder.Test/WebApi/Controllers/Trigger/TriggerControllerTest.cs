using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

[TestClass]
public class TriggerControllerTest
{
    [TestMethod]
    public async Task GetTrigger_ValidData_ReturnsTriggerDto()
    {
        var trigger = new TransportOrder.Domain.Models.Trigger.Trigger(Guid.NewGuid(), "urn", "name", "version", "description", null, []);
        var fixture = new TriggerControllerTestFixture()
            .WithTrigger(trigger)
            .Build();

        var triggerDto = await fixture.GetControllerResultValueAsync(c => c.GetTrigger(trigger.Id));

        triggerDto.Name.ShouldBe(trigger.DisplayName);
        triggerDto.Id.ShouldBe(trigger.Id);
    }

    [TestMethod]
    public async Task AssignTriggerToLocation_ValidData_ReturnsLocationDto()
    {
        var location = new TriggerLocationDto();

        var fixture = new TriggerControllerTestFixture()
            .WithTriggerLocation(location)
            .Build();

        var locationDto = await fixture.GetControllerResultAsync(c => c.AssignTriggerToLocation(Guid.NewGuid(), location.NodeId, location));
        locationDto.Id.ShouldBe(location.Id);
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
}