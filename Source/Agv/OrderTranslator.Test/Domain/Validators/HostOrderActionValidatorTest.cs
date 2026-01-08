using FluentValidation.TestHelper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Validators;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Action = Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder.Action;

namespace Gebhardt.Acc.OrderTranslator.Test.Domain.Validators;

[TestClass]
public class HostOrderActionValidatorTest
{
    [TestMethod]
    public async Task ValidateAsync_PickActionIsValid_Passes()
    {
        var hostOrderAction = new Action(Guid.NewGuid(),
            Guid.NewGuid(),
            OrderGenerator.Ps01.InputLanes.First(),
            ActionType.Pick,
            0,
            new Dictionary<InfoToAgvKey, string>());

        var validator = new HostOrderActionValidator(OrderGenerator.TestConfiguration);

        TestValidationResult<Action>? result = await validator.TestValidateAsync(hostOrderAction);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task ValidateAsync_DropActionIsValid_Passes()
    {
        var hostOrderAction = new Action(Guid.NewGuid(),
            Guid.NewGuid(),
            OrderGenerator.Ps01.OutputLanes.First(),
            ActionType.Drop,
            1,
            new Dictionary<InfoToAgvKey, string>());

        var validator = new HostOrderActionValidator(OrderGenerator.TestConfiguration);

        TestValidationResult<Action>? result = await validator.TestValidateAsync(hostOrderAction);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [DataRow(ActionType.Drop, nameof(ValidationError.TargetUnknown))]
    [DataRow(ActionType.Pick, nameof(ValidationError.SourceUnknown))]
    [TestMethod]
    public async Task ValidateAsync_ActionHasUnknownNode_Fails(ActionType actionType, string errorCode)
    {
        var config = OrderGenerator.TestConfiguration;
        var hostOrderAction = new Action(Guid.NewGuid(),
            Guid.NewGuid(),
            "unknown",
            actionType,
            0,
            new Dictionary<InfoToAgvKey, string>());

        var validator = new HostOrderActionValidator(config);

        TestValidationResult<Action>? result = await validator.TestValidateAsync(hostOrderAction);

        result.ShouldHaveValidationErrorFor(a => a.Node).WithErrorCode(errorCode);
    }
}