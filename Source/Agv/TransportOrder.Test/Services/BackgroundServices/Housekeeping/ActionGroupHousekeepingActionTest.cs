using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.BackgroundServices.Housekeeping;

[TestClass]
public class ActionGroupHousekeepingActionTest
{
    [TestMethod]
    public void EntityType_ReturnsActionGroups()
    {
        var action = new ActionGroupHousekeepingAction(new Mock<IActionGroupRepository>().Object);

        action.EntityType.ShouldBe(HousekeepingEntityType.ActionGroups);
    }

    [TestMethod]
    public async Task ExecuteAsync_CallsRepository_WithOutdatedTime()
    {
        var repository = new Mock<IActionGroupRepository>();
        var action = new ActionGroupHousekeepingAction(repository.Object);
        var outdatedTime = DateTime.UtcNow.AddDays(-90);

        await action.ExecuteAsync(outdatedTime);

        repository.Verify(r => r.DeleteOutdatedActionGroupsAsync(outdatedTime), Times.Once);
    }
}
