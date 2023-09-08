using FluentValidation.TestHelper;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = TransportOrder.Domain.Models.ActionGroup.AgvAction;

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
        var actions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(targetNodeId: "Park1") };
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions, orderId: orderId);
        
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.OrderId).WithErrorMessage("Order Id must not be null or empty.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAssignedAgvDoesNotExistInRegistry_Fails()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv();
        actionGroup.AssignedAgv = agv.CorrelationId;
        var fixture = new ActionGroupValidatorTestFixture()
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(p => p.AssignedAgv).WithErrorMessage("Assigned Agv must exist in the registry.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions).WithErrorMessage("There can be no more than 1 Actions for an action group that is triggered by a manual action.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x.AssignedAgv).WithErrorMessage("Assigned Agv must not be null or empty if the action group was triggered manually.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAssignedAgvNotNullAndIsManualIsFalse_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(isManual: false);
        actionGroup.AssignedAgv = agv.CorrelationId;

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.IsManual).WithErrorMessage("Is Manual must be true if an Agv is assigned to the action group.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x.AssignedAgv).WithErrorMessage("Assigned Agv must exist in the registry.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenActionsAreEmpty_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GenerateActionGroup(new List<AgvAction>(), correlationId:new Guid());
        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions).WithErrorMessage("Actions must not be null or empty.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x.CorrelationId).WithErrorMessage("Correlation Id must not be null or empty.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenStateOfActionGroupIsNotCreated_Fails()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        actionGroup.StartAction(actionGroup.Actions.First().CorrelationId); // manually setting to not Created state
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.State).WithErrorMessage("State must be in the Created state.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("There must be at least one Agv that knows the complete route of all actions within the given ActionGroup.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenCancelled_ThrowsOperationCancelledException()
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
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await fixture.ActionGroupValidator.TestValidateAsync(actionGroup, cancellationToken: cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task ValidateAsync_WhenAnyParentIdNotMatchCorrelationId_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actions = new List<AgvAction> { AgvActionGenerator.GenerateParkAction(parentId: "DifferentActionGroup".ToGuid(), targetNodeId: "Park1") };
        var actionGroup = AgvActionGenerator.GenerateActionGroup(actions, correlationId:"ActionGroup".ToGuid());

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("ParentId of each Action must match CorrelationId of ActionGroup.");
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

        validationResult.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("There must be at least one Agv that knows the complete route of all actions within the given ActionGroup.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenDropActionIsPresentButNoPickAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial:2);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick and drop actions must match.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenPickActionIsPresentButNoDropAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial:1);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick and drop actions must match.");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenPickActionSequenceIsAfterDropAction_Fails()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(partial:3);

        var fixture = new ActionGroupValidatorTestFixture()
            .WithAgv(agv)
            .WithRoute(agv, actionGroup)
            .WithAgvCanHandleRoute(agv, actionGroup)
            .WithNodesExisting(actionGroup)
            .Build();

        TestValidationResult<ActionGroup> validationResult = await fixture.ActionGroupValidator.TestValidateAsync(actionGroup);

        validationResult.ShouldHaveValidationErrorFor(x => x.Actions)
            .WithErrorMessage("Pick and drop actions must match.");
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

        validationResult.ShouldHaveValidationErrorFor("Actions[0].State").WithErrorMessage("State must be in the Created state.");
    }
}