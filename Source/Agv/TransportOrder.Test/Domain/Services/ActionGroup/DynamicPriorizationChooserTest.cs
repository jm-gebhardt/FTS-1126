using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class DynamicPriorizationChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_Automatic_SelectsAccordingToAgeAndCost()
    {
        var now = DateTime.UtcNow;
        var oldestSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id, createdDate: now.AddMinutes(-2));
        var oldestFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate: now.AddMinutes(-2));
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id, createdDate: now.AddSeconds(-30));
        var oldFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate: now.AddSeconds(-30));
        var newerSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id, createdDate: now);
        var newerFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate: now);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<DynamicPriorisationChooser> fixture = new ChooseAgvActionGroupTestFixture<DynamicPriorisationChooser>()
            .WithNotStartedActionGroup(oldestSlowActionGroup)
            .WithNotStartedActionGroup(oldestFastActionGroup)
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(oldFastActionGroup)
            .WithNotStartedActionGroup(newerSlowActionGroup)
            .WithNotStartedActionGroup(newerFastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldestSlowActionGroup.Actions.First().Target.NodeId!, oldestSlowActionGroup.Actions.Last().Target.NodeId!, oldestSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, oldestFastActionGroup.Actions.First().Target.NodeId!, oldestFastActionGroup.Actions.Last().Target.NodeId!, oldestFastActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.NodeId!, oldSlowActionGroup.Actions.Last().Target.NodeId!, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, oldFastActionGroup.Actions.First().Target.NodeId!, oldFastActionGroup.Actions.Last().Target.NodeId!, oldFastActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, newerSlowActionGroup.Actions.First().Target.NodeId!, newerSlowActionGroup.Actions.Last().Target.NodeId!, newerSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, newerFastActionGroup.Actions.First().Target.NodeId!, newerFastActionGroup.Actions.Last().Target.NodeId!, newerFastActionGroup, 1f)
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldestFastActionGroup.CorrelationId);
    }
}