using FluentValidation.TestHelper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Validators;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Validators;

[TestClass]
public class HostOrderValidatorTest
{
    [TestMethod]
    public async Task ValidateAsync_HostOrderIsValid_Passes()
    {
        var config = OrderGenerator.KaeserConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction(config.StationMapping.Keys.First(), ActionType.Pick);
        hostOrder.AddAction(config.StationMapping.Keys.Last(), ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        TestValidationResult<HostOrder> result = await validator.TestValidateAsync(hostOrder);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidSource_Fails()
    {
        var config = OrderGenerator.KaeserConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction("unknown", ActionType.Pick);
        hostOrder.AddAction(config.StationMapping.Keys.Last(), ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        TestValidationResult<HostOrder> result = await validator.TestValidateAsync(hostOrder);

        result.IsValid.ShouldBeFalse();
        result.Errors.First().ErrorCode.ShouldBe(KaeserGraphConfiguration.ErrorCodeSourceUnknown.ToString());
    }

    [TestMethod]
    public async Task ValidateAsync_InvalidTarget_Fails()
    {
        var config = OrderGenerator.KaeserConfiguration;
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow);
        hostOrder.AddAction(config.StationMapping.Keys.First(), ActionType.Pick);
        hostOrder.AddAction("unknown", ActionType.Drop);

        var validator = new HostOrderValidator(new HostOrderActionValidator(config));

        TestValidationResult<HostOrder> result = await validator.TestValidateAsync(hostOrder);

        result.IsValid.ShouldBeFalse();
        result.Errors.First().ErrorCode.ShouldBe(KaeserGraphConfiguration.ErrorCodeTargetUnknown.ToString());
    }
}