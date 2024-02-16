using FluentValidation.TestHelper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Validators;

[TestClass]
public class ActionGroupValidatorTest
{
    [TestMethod]
    public async Task ValidateAsync_WhenAllConditionsAreMet_Passes()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task ValidateAsync_WhenOrderIdIsNullOrEmpty_Fails(string orderId)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction("Park1") };
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions, orderId: orderId);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.OrderId).WithErrorMessage("OrderId is null or empty");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAssignedAgvDoesNotExistInRepository_Fails()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;
        var fixture = new ActionGroupValidatorTestFixture()
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(p => p.AssignedAgv).WithErrorMessage("Agv with correlation id \"1b132881-d347-9be6-2ae6-ae180760d3da\" does not exist");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenIsManualIsTrueAndActionsCountIsMoreThanOne_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.IsManual = true;

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions).WithErrorMessage("Manual action must have exactly one action");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenIsManualIsTrueAndNoAssignedAgv_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();
        actionGroup.IsManual = true;
        actionGroup.AssignedAgv = null;

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.AssignedAgv).WithErrorMessage("Manual action must have an Agv assigned");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAssignedAgvNotNullAndAgvDoesNotExist_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(isManual: true);
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = new ActionGroupValidatorTestFixture()
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.AssignedAgv).WithErrorMessage("Agv with correlation id \"1b132881-d347-9be6-2ae6-ae180760d3da\" does not exist");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenActionsAreEmpty_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(new List<AgvAction>(), correlationId: new Guid());
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions).WithErrorMessage("Actions must be set");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenCorrelationIdIsEmpty_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(parentId: null, targetNodeId: "Park1") };
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions, correlationId: Guid.Empty);
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.CorrelationId).WithErrorMessage("CorrelationId is null or empty");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenStateOfActionGroupIsNotCreated_Fails()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId, ["source", "way", "target"]); // manually setting to not Created state
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.State).WithErrorMessage("Action group state must be created, but was Started");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenInvalidRouteExists_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("There must be at least one Agv that knows the complete route of all actions within the given ActionGroup");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenCanceled_ThrowsOperationCanceledException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .Build();

        var cancellationTokenSource = new CancellationTokenSource();
        // Immediately cancel the token
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await fixture.ActionGroupValidator.TestValidateAsync(actionGroup, cancellationToken: cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAnyParentIdNotMatchCorrelationId_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(parentId: "DifferentActionGroup".ToGuid(), targetNodeId: "Park1") };
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions, correlationId: "ActionGroup".ToGuid());

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Parent id of each action must match correlation id of action group");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenNoAgvKnowsTheCompleteRoute_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("There must be at least one Agv that knows the complete route of all actions within the given ActionGroup");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenDropActionIsPresentButNoPickAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial: 2);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick/Drop sequence must be valid");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenPickActionIsPresentButNoDropAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial: 1);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick/Drop sequence must be valid");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenPickActionSequenceIsAfterDropAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial: 3);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick/Drop sequence must be valid");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenStateOfActionIsNotCreated_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.CancelActionGroup();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor("Actions[0].State").WithErrorMessage("Action state must be created, but was Canceled");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenSequenceIsNotSet_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var orderId = Guid.NewGuid();
        var testActions = new List<AgvAction>
        {
            new(Guid.NewGuid(), ActionType.Pick, "S1", orderId),
            new(Guid.NewGuid(), ActionType.Drop, "S2", orderId)
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.Simulation,
            DateTime.UtcNow);
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(a => a).WithErrorMessage("Action sequence Ids must be set.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenSequenceIdsAreNotUnique_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var orderId = Guid.NewGuid();
        var testActions = new List<AgvAction>
        {
            new(Guid.NewGuid(), ActionType.Pick, "S1", orderId)
            {
                Sequence = 2
            },
            new(Guid.NewGuid(), ActionType.Drop, "S2", orderId)
            {
                Sequence = 2
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.Simulation,
            DateTime.UtcNow);
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(a => a).WithErrorMessage("Action sequence Ids must be unique");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenSequenceHasGaps_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var orderId = Guid.NewGuid();
        var testActions = new List<AgvAction>
        {
            new(Guid.NewGuid(), ActionType.Pick, "S1", orderId)
            {
                Sequence = 0
            },
            new(Guid.NewGuid(), ActionType.Drop, "S2", orderId)
            {
                Sequence = 2
            }
        };

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.Simulation,
            DateTime.UtcNow);
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(a => a).WithErrorMessage("Action sequence must not have gaps");
    }
}