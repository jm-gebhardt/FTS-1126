using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using AgvActionGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

[TestClass]
public class ResendActionRequestHandlerTest
{
    [TestMethod]
    public async Task ResendAction_ValidData_PublishesResendAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ResendActionRequestHandlerTestFixture<ResendAction>()
            .AddAgvs([agv])
            .AddActionGroup(actionGroup)
            .Build();

        await fixture.GetResponseAsync<Success>(new ResendAction(agv.CorrelationId, "user"));

        await fixture.ShouldHavePublishedAsync<Common.Messages.ActionGroups.Commands.ResendAction>();
    }

    [TestMethod]
    public async Task ResendAction_ActionGroupNotExisting_RespondsWithActionNotFound()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ResendActionRequestHandlerTestFixture<ResendAction>()
            .AddAgvs([agv])
            .AddActionGroup(null)
            .Build();

        await fixture.GetResponseAsync<ActionNotFound>(new ResendAction(agv.CorrelationId, "user"));
    }

    [TestMethod]
    public async Task ResendAction_AllActionsCompleted_RespondsWithActionNotFound()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionState: ActionState.Completed);
        var fixture = new ResendActionRequestHandlerTestFixture<ResendAction>()
            .AddAgvs([agv])
            .AddActionGroup(actionGroup)
            .Build();

        await fixture.GetResponseAsync<ActionNotFound>(new ResendAction(agv.CorrelationId, "user"));
    }

    [TestMethod]
    public async Task ResendAction_TargetNotSet_ThrowsNotInCorrectStateException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionState: ActionState.Completed);
        actionGroup.Actions.Last().State = ActionState.Created;
        var fixture = new ResendActionRequestHandlerTestFixture<ResendAction>()
            .AddAgvs([agv])
            .AddActionGroup(actionGroup)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(fixture.GetResponseAsync<Success>(new ResendAction(agv.CorrelationId, "user")));
    }
}