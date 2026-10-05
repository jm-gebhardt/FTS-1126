using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class DynamicPrioritizationChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_Automatic_SelectsAccordingToAgeAndCost()
    {
        var now = DateTime.UtcNow;
        var oldestSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now.AddMinutes(-2));
        var oldestFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now.AddMinutes(-2));
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now.AddSeconds(-30));
        var oldFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now.AddSeconds(-30));
        var newerSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now);
        var newerFastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<DynamicPrioritizationChooser>()
            .WithNotStartedActionGroup(oldestSlowActionGroup)
            .WithNotStartedActionGroup(oldestFastActionGroup)
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(oldFastActionGroup)
            .WithNotStartedActionGroup(newerSlowActionGroup)
            .WithNotStartedActionGroup(newerFastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldestSlowActionGroup.Actions.First().Target.Node!.Name, oldestSlowActionGroup.Actions.Last().Target.Node!.Name, oldestSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, oldestFastActionGroup.Actions.First().Target.Node!.Name, oldestFastActionGroup.Actions.Last().Target.Node!.Name, oldestFastActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.Node!.Name, oldSlowActionGroup.Actions.Last().Target.Node!.Name, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, oldFastActionGroup.Actions.First().Target.Node!.Name, oldFastActionGroup.Actions.Last().Target.Node!.Name, oldFastActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, newerSlowActionGroup.Actions.First().Target.Node!.Name, newerSlowActionGroup.Actions.Last().Target.Node!.Name, newerSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, newerFastActionGroup.Actions.First().Target.Node!.Name, newerFastActionGroup.Actions.Last().Target.Node!.Name, newerFastActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldestFastActionGroup.CorrelationId);
    }
}