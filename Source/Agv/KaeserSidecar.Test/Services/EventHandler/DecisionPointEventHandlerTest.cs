using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DecisionPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_ValidData_EnqueuesAction()
    {
        var dropActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(
            actionGroupState: ActionGroupState.Started, actionState: ActionState.Started, assignedAgv: Guid.NewGuid(), 
            moveDestination:OrderGenerator.KaeserConfiguration.LongLoopMoveTargetNode);
        const bool useJamState = true;
        var fixture = new DecisionPointEventHandlerTestFixture();
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(){CorrelationId = dropActionGroup.AssignedAgv!.Value }, dropActionGroup, [], [], 3, useJamState);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        fixture.VerifyActionEnqueued(dropActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, dropActionGroup.AssignedAgv!.Value, Times.Once());
        fixture.VerifyStationCountSet(3, Times.Once());
        fixture.VerifyUseJamStateSet(useJamState, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AklPickOrder_ActionNotEnqueued()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
       
        var fixture = new DecisionPointEventHandlerTestFixture();
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], [], 3, false);
        
        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        fixture.VerifyNoActionEnqueued();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AklDropOrder_ActionNotEnqueued()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());

        var fixture = new DecisionPointEventHandlerTestFixture();
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus() { CorrelationId = actionGroup.AssignedAgv!.Value }, actionGroup, [], [], 3, false);
        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        fixture.VerifyNoActionEnqueued();
    }
}