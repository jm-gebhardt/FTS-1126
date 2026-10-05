using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

[TestClass]
public class TriggersControllerTest
{
    [TestMethod]
    public async Task GetTriggers_ValidData_ReturnsTriggerDtos()
    {
        var triggers = TriggerGenerator.CreateTriggers(3);
        var fixture = new TriggersControllerTestFixture()
            .WithTriggers(triggers)
            .Build();

        var triggerDtos = await fixture.GetControllerResultValueAsync(c => c.GetTriggers());

        triggerDtos.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task GetTriggers_EmptyList_ReturnsEmptyCollection()
    {
        var fixture = new TriggersControllerTestFixture()
            .WithTriggers([])
            .Build();

        var triggerDtos = await fixture.GetControllerResultValueAsync(c => c.GetTriggers());

        triggerDtos.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetDeletedTriggers_ValidData_ReturnsDeletedTriggerDtos()
    {
        var deletedTriggers = new List<TransportOrder.Domain.Models.Trigger.Trigger>
        {
            TriggerGenerator.CreateTrigger(isDeleted: true),
            TriggerGenerator.CreateTrigger(isDeleted: true)
        };
        var fixture = new TriggersControllerTestFixture()
            .WithDeletedTriggers(deletedTriggers)
            .Build();

        var triggerDtos = await fixture.GetControllerResultValueAsync(c => c.GetDeletedTriggers());

        triggerDtos.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetDeletedTriggers_EmptyList_ReturnsEmptyCollection()
    {
        var fixture = new TriggersControllerTestFixture()
            .WithDeletedTriggers([])
            .Build();

        var triggerDtos = await fixture.GetControllerResultValueAsync(c => c.GetDeletedTriggers());

        triggerDtos.ShouldBeEmpty();
    }
}