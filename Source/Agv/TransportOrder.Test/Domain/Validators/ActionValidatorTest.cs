using FluentValidation.TestHelper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Validators;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Validators;

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

        validationResult.ShouldHaveValidationErrorFor(x => x.State).WithErrorMessage("Action state must be created, but was Canceled");
    }
}