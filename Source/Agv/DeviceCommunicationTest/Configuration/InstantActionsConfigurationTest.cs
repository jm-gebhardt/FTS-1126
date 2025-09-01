using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Configuration;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Configuration;

[TestClass]
public class InstantActionsConfigurationTest
{
    [TestMethod]
    public void GetDefinitionsForAgv_NoDefinitions_ReturnsEmpty()
    {
        const string agvSerialNumber = "serial number";
        var config = new InstantActionsConfiguration();
        var agvData = AgvDataGenerator.GenerateAgvData(agvSerialNumber.ToGuid());

        config.GetDefinitionsForAgv(agvData, agvSerialNumber).ShouldBeEmpty();
    }

    [TestMethod]
    public void GetDefinitionsForAgv_NoAgvTypeOrSerialNumberGiven_ReturnsAllDefinitions()
    {
        const string agvSerialNumber = "serial number";
        var agvData = AgvDataGenerator.GenerateAgvData(agvSerialNumber.ToGuid());
        var config = new InstantActionsConfiguration([new AgvInstantActionConfiguration(), new AgvInstantActionConfiguration()]);

        config.GetDefinitionsForAgv(agvData, agvSerialNumber).Count.ShouldBe(config.InstantActions.Count);
    }

    [TestMethod]
    public void GetDefinitionsForAgv_AgvSerialNumberGiven_ReturnsDefinitionForAgv()
    {
        const string agvSerialNumber = "serial number";
        var instantActionForAgv = new AgvInstantActionConfiguration()
        {
            AgvSerialNumbers = [agvSerialNumber],
            ActionType = "action for agv"
        };
        var agvData = AgvDataGenerator.GenerateAgvData(agvSerialNumber.ToGuid());
        var config = new InstantActionsConfiguration([instantActionForAgv, new AgvInstantActionConfiguration() { AgvSerialNumbers = ["another agv"] }]);

        config.GetDefinitionsForAgv(agvData, agvSerialNumber).Single().ActionType.ShouldBeEquivalentTo(instantActionForAgv.ActionType);
    }

    [TestMethod]
    public void GetDefinitionsForAgv_AgvTypeGiven_ReturnsDefinitionForAgv()
    {
        const string agvSerialNumber = "serial number";
        var agvData = AgvDataGenerator.GenerateAgvData(agvSerialNumber.ToGuid());

        var instantActionForAgv = new AgvInstantActionConfiguration()
        {
            AgvTypes = [new AgvType()
            {
                Model=agvData.Model,
                Type = agvData.Type
            }],
            ActionType = "action for agv"
        };
        var config = new InstantActionsConfiguration([instantActionForAgv, new AgvInstantActionConfiguration() { AgvSerialNumbers = ["another agv"] }]);

        config.GetDefinitionsForAgv(agvData, agvSerialNumber).Single().ActionType.ShouldBeEquivalentTo(instantActionForAgv.ActionType);
    }

    [TestMethod]
    public void GetDefinitionsForAgv_AgvTypeGivenButAgvIsExcluded_DoesNotReturnDefinition()
    {
        const string agvSerialNumber = "serial number";
        var agvData = AgvDataGenerator.GenerateAgvData(agvSerialNumber.ToGuid());

        var instantActionForAgv = new AgvInstantActionConfiguration()
        {
            AgvTypes = [new AgvType()
            {
                Model=agvData.Model,
                Type = agvData.Type,
                ExcludedAgvSerialNumbers = [agvSerialNumber]
            }],
            ActionType = "action for agv"
        };
        var config = new InstantActionsConfiguration([instantActionForAgv, new AgvInstantActionConfiguration() { AgvSerialNumbers = ["another agv"] }]);

        config.GetDefinitionsForAgv(agvData, agvSerialNumber).ShouldBeEmpty();
    }
}