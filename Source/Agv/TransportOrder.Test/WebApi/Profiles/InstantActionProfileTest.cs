using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class InstantActionProfileTest
{
    [TestMethod]
    public void Map_AgvInstantActionDefinition_ShouldMapAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new AgvInstantActionProfile());
        var agvInstantActionDefinition = new InstantActionDefinition
        {
            Description = "description",
            ResultDescription = "resultDescription",
            ActionType = "actionType",
            BlockingTypes = [BlockingType.HARD],
            Parameters = [new AgvInstantActionParameterDefinition()
            {
                Description = "Parameter description",
                IsOptional = false,
                Key = "parameter key",
                ValueType = "String"
            }]
        };

        var agvInstantActionDefinitionDto = mapper.Map<AgvInstantActionDefinitionDto>(agvInstantActionDefinition);
        agvInstantActionDefinitionDto.Description.ShouldBe(agvInstantActionDefinition.Description);
    }

    [TestMethod]
    public void Map_AgvInstantActionDefinitions_ShouldMapAllProperties()
    {
        var mapper = MappingHelper.GetMapper(new AgvInstantActionProfile());
        var agvInstantActionDefinition = new InstantActionDefinition
        {
            Description = "description",
            ResultDescription = "resultDescription",
            ActionType = "actionType",
            BlockingTypes = [BlockingType.HARD],
            Parameters = [new AgvInstantActionParameterDefinition()
            {
                Description = "Parameter description",
                IsOptional = false,
                Key = "parameter key",
                ValueType = "String"
            }]
        };

        var agvInstantActionDefinitionDto = mapper.Map<ICollection<AgvInstantActionDefinitionDto>>(new List<InstantActionDefinition>{agvInstantActionDefinition});
        agvInstantActionDefinitionDto.First().Description.ShouldBe(agvInstantActionDefinition.Description);
    }
}