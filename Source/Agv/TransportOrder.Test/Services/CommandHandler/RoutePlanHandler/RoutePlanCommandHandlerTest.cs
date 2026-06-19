using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.RoutePlanHandler;

[TestClass]
public class RoutePlanCommandHandlerTest
{
    private const string SerialNumber = "agv1";
    private const string BaseNodeName = "WAY1";

    private static RoutePlan CreateRoutePlan() =>
        new([new GraphNode(BaseNodeName.ToGuid(), BaseNodeName)], [], [], []);

    [TestMethod]
    public async Task Consume_DispatchRouteCommand_CachesPlan()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new RoutePlanCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new DispatchRouteCommand(agvCorrelationId, SerialNumber, CreateRoutePlan(), new AgvAction()));

        fixture.RoutePlanCache.ConcurrentDict.ShouldContainKey(agvCorrelationId);
        fixture.RoutePlanCache.ConcurrentDict[agvCorrelationId].BaseNodes.Single().Name.ShouldBe(BaseNodeName);
    }

    [TestMethod]
    public async Task Consume_UpdateDispatchedRouteCommand_OverwritesCachedPlan()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new RoutePlanCommandHandlerTestFixture();
        fixture.RoutePlanCache.ConcurrentDict[agvCorrelationId] = CreateRoutePlan();

        await fixture.PublishMessageAsync(new UpdateDispatchedRouteCommand(agvCorrelationId, SerialNumber, new RoutePlan([], [], [], []), new AgvAction()));

        fixture.RoutePlanCache.ConcurrentDict[agvCorrelationId].BaseNodes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_RemovesCachedPlan()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var fixture = new RoutePlanCommandHandlerTestFixture();
        fixture.RoutePlanCache.ConcurrentDict[agvCorrelationId] = CreateRoutePlan();

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        fixture.RoutePlanCache.ConcurrentDict.ShouldNotContainKey(agvCorrelationId);
    }
}