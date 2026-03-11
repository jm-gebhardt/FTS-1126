using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Shouldly;
using InstantActionDefinition = Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition.InstantActionDefinition;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class InstantActionDefinitionTest
{
    [TestMethod]
    public void ValidateAgv_AgvCanNotRunAction_ThrowsAgvInstantActionDefinitionNotFoundException()
    {
        var action = new InstantActionDefinition();
        action.AddAgv(Guid.NewGuid());

        Should.Throw<AgvInstantActionDefinitionNotFoundException>(() => action.ValidateAgv(Guid.NewGuid()));
    }

    [TestMethod]
    public void ValidateBlockingType_NoBlockingTypeGivenButIsNeeded_ThrowsInvalidInstantActionBlockingTypeException()
    {
        var action = new InstantActionDefinition { BlockingTypes = [BlockingType.HARD] };
        Should.Throw<InvalidInstantActionBlockingTypeException>(() => action.ValidateBlockingType(null));
    }

    [TestMethod]
    public void ValidateBlockingType_BlockingTypeGivenButNoneDefined_ThrowsInvalidInstantActionBlockingTypeException()
    {
        var action = new InstantActionDefinition { BlockingTypes = null };
        Should.Throw<InvalidInstantActionBlockingTypeException>(() => action.ValidateBlockingType(BlockingType.HARD));
    }

    [TestMethod]
    public void ValidateBlockingType_UnsupportedBlockingTypeGiven_ThrowsInvalidInstantActionBlockingTypeException()
    {
        var action = new InstantActionDefinition { BlockingTypes = [BlockingType.HARD] };
        Should.Throw<InvalidInstantActionBlockingTypeException>(() => action.ValidateBlockingType(BlockingType.SOFT));
    }

    [TestMethod]
    public void ValidateParameters_ParameterValuesGivenButNoneDefined_ThrowsInstantActionParameterValueNotFoundException()
    {
        var action = new InstantActionDefinition { Parameters = null };
        Should.Throw<InstantActionParameterValueNotFoundException>(() => action.ValidateParameters(new Dictionary<string, object> { { "key", "value" } }));
    }

    [TestMethod]
    public void ValidateParameters_RequiredParameterValuesNotGiven_ThrowsInstantActionParameterValueNotFoundException()
    {
        var action = new InstantActionDefinition { Parameters = [new AgvInstantActionParameterDefinition() { Key = "Key", IsOptional = false }] };
        Should.Throw<InstantActionParameterValueNotFoundException>(() => action.ValidateParameters(new Dictionary<string, object>()));
    }

    [TestMethod]
    public void ValidateParameters_UnknownParameterValuesGiven_ThrowsInvalidInstantActionParameterException()
    {
        var action = new InstantActionDefinition { Parameters = [new AgvInstantActionParameterDefinition() { Key = "Key", IsOptional = true }] };
        Should.Throw<InvalidInstantActionParameterException>(() => action.ValidateParameters(new Dictionary<string, object> { { "unknown key", "value" } }));
    }
}