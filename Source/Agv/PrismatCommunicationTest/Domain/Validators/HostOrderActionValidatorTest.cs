using FluentValidation.TestHelper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Validators;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Validators;

[TestClass]
public class HostOrderActionValidatorTest
{
    [DataRow(ActionType.Pick)]
    [DataRow(ActionType.Drop)]
    [TestMethod]
    public async Task ValidateAsync_ActionIsValid_Passes(ActionType actionType)
    {
        var config = OrderGenerator.KaeserConfiguration;
        var hostOrderAction = new PrismatCommunication.Domain.Models.HostOrder.Action(Guid.NewGuid(),
            Guid.NewGuid(),
            config.StationMapping.Keys.First(),
            actionType,
            0,
            new Dictionary<InfoToAgvKey, string>());

        var validator = new HostOrderActionValidator(config);

        TestValidationResult<PrismatCommunication.Domain.Models.HostOrder.Action> result = await validator.TestValidateAsync(hostOrderAction);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [DataRow(ActionType.Drop, KaeserGraphConfiguration.ErrorCodeTargetUnknown)]
    [DataRow(ActionType.Pick, KaeserGraphConfiguration.ErrorCodeSourceUnknown)]
    [TestMethod]
    public async Task ValidateAsync_ActionHasUnknownNode_Fails(ActionType actionType, int errorCode)
    {
        var config = OrderGenerator.KaeserConfiguration;
        var hostOrderAction = new PrismatCommunication.Domain.Models.HostOrder.Action(Guid.NewGuid(),
            Guid.NewGuid(),
            "unknown",
            actionType,
            0,
            new Dictionary<InfoToAgvKey, string>());

        var validator = new HostOrderActionValidator(config);

        TestValidationResult<PrismatCommunication.Domain.Models.HostOrder.Action> result = await validator.TestValidateAsync(hostOrderAction);

        result.ShouldHaveValidationErrorFor(a => a.Node).WithErrorCode(errorCode.ToString());
    }
}