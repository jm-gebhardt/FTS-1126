using FluentValidation.TestHelper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Validators;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test.Domain.Validators;

[TestClass]
public class HostOrderValidatorTest
{
    [TestMethod]
    public async Task ValidateAsync_HostOrderIsValid_Passes()
    {
        var config = OrderGenerator.TestConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction(OrderGenerator.Ps01.InputLanes.First(), ActionType.Pick);
        hostOrder.AddAction(OrderGenerator.Apl1.OutputLanes.First(), ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        var result = await validator.TestValidateAsync(hostOrder);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidSource_Fails()
    {
        var config = OrderGenerator.TestConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction("unknown", ActionType.Pick);
        hostOrder.AddAction(OrderGenerator.Apl1.OutputLanes.First(), ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        var result = await validator.TestValidateAsync(hostOrder);

        result.IsValid.ShouldBeFalse();
        result.Errors.First().ErrorCode.ShouldBe(nameof(ValidationError.SourceUnknown));
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidTarget_Fails()
    {
        var config = OrderGenerator.TestConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction(OrderGenerator.Ps01.InputLanes.First(), ActionType.Pick);
        hostOrder.AddAction("unknown", ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        var result = await validator.TestValidateAsync(hostOrder);

        result.IsValid.ShouldBeFalse();
        result.Errors.First().ErrorCode.ShouldBe(nameof(ValidationError.TargetUnknown));
    }
}