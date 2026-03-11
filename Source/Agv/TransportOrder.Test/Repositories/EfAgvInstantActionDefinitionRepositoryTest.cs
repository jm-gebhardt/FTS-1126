using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvInstantActionDefinitionRepositoryTest
{
    [TestMethod]
    public async Task GetInstantActionDefinitionsForAgv_NoDefinitionsForAgv_ReturnsEmpty()
    {
        await using var fixture = new EfAgvInstantActionDefinitionRepositoryTestFixture();
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
        agvInstantActionDefinition.AddAgv(Guid.NewGuid());

        await fixture.Repository.Add(agvInstantActionDefinition);

        (await fixture.Repository.GetInstantActionDefinitionsForAgv(Guid.NewGuid())).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetInstantActionDefinitionsForAgv_ValidData_ReturnsInstantActionDefinitions()
    {
        await using var fixture = new EfAgvInstantActionDefinitionRepositoryTestFixture();
        var agv = Guid.NewGuid();
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
        agvInstantActionDefinition.AddAgv(agv);

        await fixture.Repository.Add(agvInstantActionDefinition);

        (await fixture.Repository.GetInstantActionDefinitionsForAgv(agv)).ShouldBeEquivalentTo(new List<InstantActionDefinition> { agvInstantActionDefinition });
    }

    [TestMethod]
    public async Task GetInstantActionDefinitionsByAgv_NoDefinitionsForAgv_ReturnsEmptyListForAgv()
    {
        await using var fixture = new EfAgvInstantActionDefinitionRepositoryTestFixture();
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
        agvInstantActionDefinition.AddAgv(Guid.NewGuid());

        await fixture.Repository.Add(agvInstantActionDefinition);

        var agv = Guid.NewGuid();
        (await fixture.Repository.GetInstantActionDefinitionsByAgv([agv]))[agv].ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetInstantActionDefinitionsByAgv_ValidData_ReturnsInstantActionDefinitionsByAgv()
    {
        await using var fixture = new EfAgvInstantActionDefinitionRepositoryTestFixture();
        var agv = Guid.NewGuid();
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
        agvInstantActionDefinition.AddAgv(agv);

        await fixture.Repository.Add(agvInstantActionDefinition);

        (await fixture.Repository.GetInstantActionDefinitionsByAgv([agv]))[agv].ShouldBeEquivalentTo(new List<InstantActionDefinition> { agvInstantActionDefinition });
    }
}