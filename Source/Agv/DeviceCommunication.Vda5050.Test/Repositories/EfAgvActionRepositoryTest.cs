using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

[TestClass]
public class EfAgvActionRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateActionsAsync_ExistingAction_UpdatesDataWithoutTrackingConflict()
    {
        var fixture = new AgvActionRepositoryTestFixture();
        const string serialNumber = "agv-serial";

        var existingAction = ActionGenerator.CreateAction(serialNumber, "pick", "existing", [ActionScope.Node], [BlockingType.SOFT], "parameter-1");
        await fixture.AddAsync(existingAction);

        var updatedAction = ActionGenerator.CreateAction(serialNumber, "pick", "updated", [ActionScope.Edge], [BlockingType.HARD], "parameter-2");
        await fixture.Repository.AddOrUpdateActionsAsync([updatedAction], serialNumber);

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();
        var storedAction = await context.Set<AgvAction>()
            .AsNoTracking()
            .SingleAsync(action => action.AgvSerialNumber == serialNumber && action.ActionType == updatedAction.ActionType);

        storedAction.ShouldBeEquivalentTo(updatedAction);
    }

    [TestMethod]
    public async Task AddOrUpdateActionsAsync_DuplicateIncomingActionTypes_StoresLatestAction()
    {
        var fixture = new AgvActionRepositoryTestFixture();
        const string serialNumber = "agv-serial";

        var firstAction = ActionGenerator.CreateAction(serialNumber, "pick", "first", [ActionScope.Node], [BlockingType.SOFT], "parameter-1");
        var latestAction = ActionGenerator.CreateAction(serialNumber, "pick", "latest", [ActionScope.Instant], [BlockingType.HARD], "parameter-2");

        await fixture.Repository.AddOrUpdateActionsAsync([firstAction, latestAction], serialNumber);

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();
        var storedActions = await context.Set<AgvAction>()
            .AsNoTracking()
            .Where(action => action.AgvSerialNumber == serialNumber)
            .ToListAsync();

        storedActions.Count.ShouldBe(1);
        storedActions.Single().ShouldBeEquivalentTo(latestAction);
    }
}