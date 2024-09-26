using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class FastestRouteOfXOldestActionGroupsChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActionsX2_SelectsFastestOfThe2Oldest()
    {
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var oldFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var newerSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var newerFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<FastestRouteOfXOldestActionGroupsChooser> fixture = new ChooseAgvActionGroupTestFixture<FastestRouteOfXOldestActionGroupsChooser>()
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(oldFastActionGroup)
            .WithNotStartedActionGroup(newerSlowActionGroup)
            .WithNotStartedActionGroup(newerFastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.NodeId!, oldSlowActionGroup.Actions.Last().Target.NodeId!, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, oldFastActionGroup.Actions.First().Target.NodeId!, oldFastActionGroup.Actions.Last().Target.NodeId!, oldFastActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, newerSlowActionGroup.Actions.First().Target.NodeId!, newerSlowActionGroup.Actions.Last().Target.NodeId!, newerSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, newerFastActionGroup.Actions.First().Target.NodeId!, newerFastActionGroup.Actions.Last().Target.NodeId!, newerFastActionGroup, 1f)
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldFastActionGroup.CorrelationId);
    }
}