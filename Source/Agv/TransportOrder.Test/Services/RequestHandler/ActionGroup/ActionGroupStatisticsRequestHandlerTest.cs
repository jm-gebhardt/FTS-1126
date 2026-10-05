using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

[TestClass]
public class ActionGroupStatisticsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetActionGroupStatistics_NoMatchingActionGroups_RespondsGotActionGroupStatisticsWithEmptyDurationStatistics()
    {
        var fixture = new ActionGroupStatisticsRequestHandlerTestFixture()
            .WithActionDurations([])
            .Build();

        var response = await fixture.GetResponseAsync<GotActionGroupStatistics>(new GetActionGroupStatistics("a source".ToGuid(), "a target".ToGuid()));

        response.ShouldNotBeNull();
        response.Message.DurationStatistics.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetActionGroupStatistics_PickDropActionGroups_RespondsGotActionGroupStatisticsWithDurationStatistics()
    {
        var durations = Enumerable.Range(1, 10).ToList().Select(d => (double) d).ToList();
        var actionDurations = new Dictionary<ActionType, ICollection<double>>()
        {
            { ActionType.Pick, durations },
            { ActionType.Drop, durations }
        };
        var fixture = new ActionGroupStatisticsRequestHandlerTestFixture()
            .WithActionDurations(actionDurations)
            .Build();

        var response = await fixture.GetResponseAsync<GotActionGroupStatistics>(new GetActionGroupStatistics("source".ToGuid(), "target".ToGuid()));

        response.ShouldNotBeNull();
        response.Message.DurationStatistics.Count.ShouldBe(actionDurations.Count);
        response.Message.DurationStatistics.Select(d => d.ActionType).ShouldBe([ActionType.Pick, ActionType.Drop], ignoreOrder: true);
        response.Message.DurationStatistics.Single(d => d.ActionType == ActionType.Drop).ShouldBe(new DurationStatistics(ActionType.Drop, durations.Median(), durations.Min(), durations.Max()));
        response.Message.DurationStatistics.Single(d => d.ActionType == ActionType.Pick).ShouldBe(new DurationStatistics(ActionType.Pick, durations.Median(), durations.Min(), durations.Max()));
    }
}