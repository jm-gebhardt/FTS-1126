using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.ActionGroup;

[TestClass]
public class ActionGroupsControllerTest
{
    [TestMethod]
    public async Task GetActionGroups_ValidData_ReturnsActionGroupDtos()
    {
        var actionGroups = new List<ActionGroupReadModel>
        {
            new(Guid.NewGuid(),
                "orderId",
                Guid.NewGuid(),
                DateTime.UtcNow,
                null,
                null,
                null,
                ActionGroupState.Created,
                ActionGroupCreator.TransportOrder,
                [
                    new AgvActionReadModel(Guid.NewGuid(),
                        Guid.NewGuid(),
                        1,
                        ActionState.Created,
                        ActionType.Move,
                        "target",
                        AgvGraph.NodeType.PickDrop,
                        [],
                        DateTime.UtcNow,
                        null,
                        null)
                ], false, [])
        };
        var fixture = new ActionGroupsControllerTestFixture().WithActionGroups(actionGroups);

        List<ActionGroupDto> actionGroupDtos = (await fixture.GetControllerResultValueAsync(c => c.GetActionGroups([ActionGroupState.Created], null, null, null, 100))).ToList();

        actionGroupDtos.ShouldNotBeNull();
        actionGroupDtos.Count.ShouldBe(actionGroups.Count);
    }

    [TestMethod]
    public async Task GetActionGroupCountByState_ValidData_ReturnsAgvGroupCountByStateDtos()
    {
        var actionGroupCounts = new AgvGroupCountByState(ActionGroupState.Created, 1);

        var fixture = new ActionGroupsControllerTestFixture()
            .WithActionGroupCount([actionGroupCounts]);

        List<AgvGroupCountByStateDto> dtos = (await fixture.GetControllerResultValueAsync(c => c.GetActionGroupCountsByState())).ToList();

        dtos.ShouldNotBeNull();
        dtos.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetDurationStatistics_ValidData_ReturnsActionDurationStatisticsDtos()
    {
        var durationStatistics = new List<DurationStatistics>
        {
            new(ActionType.Pick,
                12,
                1,
                20),
            new(ActionType.Drop,
                2,
                13,
                22)
        };
        var fixture = new ActionGroupsControllerTestFixture()
            .WithDurationStatistics(durationStatistics);

        List<ActionDurationStatisticsDto> durationStatisticsDtos = (await fixture.GetControllerResultValueAsync(c => c.GetDurationStatistics("source", "target"))).ToList();

        durationStatisticsDtos.ShouldNotBeNull();
        durationStatisticsDtos.Count.ShouldBe(durationStatistics.Count);
        foreach (var durationStatistic in durationStatistics)
        {
            var dto = durationStatisticsDtos.Single(d => d.ActionType == durationStatistic.ActionType);
            dto.MaxDuration.ShouldBe(durationStatistic.MaxDuration);
            dto.MinDuration.ShouldBe(durationStatistic.MinDuration);
            dto.MedianDuration.ShouldBe(durationStatistic.MedianDuration);
        }
    }
}