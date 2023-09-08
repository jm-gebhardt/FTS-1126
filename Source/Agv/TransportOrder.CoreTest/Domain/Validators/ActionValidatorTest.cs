using FluentValidation.TestHelper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransportOrder.Domain.Validators;
using AgvAction = TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Validators;

[TestClass]
public class AgvActionValidatorTest
{
    [TestMethod]
    public async Task ValidateAsync_WhenEverythingIsValid_Passes()
    {
        var action = AgvActionGenerator.GenerateParkAction(targetNodeId: null);

        var agvActionValidator = new AgvActionValidator();

        TestValidationResult<AgvAction> validationResult = await agvActionValidator.TestValidateAsync(action);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task ValidateAsync_WhenStateIsNotNullButNotCreated_Fails()
    {
        var action = AgvActionGenerator.GenerateParkAction();
        action.UpdateActionState(ActionState.Canceled);

        var agvActionValidator = new AgvActionValidator();

        TestValidationResult<AgvAction> validationResult = await agvActionValidator.TestValidateAsync(action);

        validationResult.ShouldHaveValidationErrorFor(x => x.State).WithErrorMessage("State must be in the Created state.");
    }
}