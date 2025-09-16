using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class FastestRouteAndThresholdChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActionsYoungerThanThreshold_SelectsAccordingToCosts()
    {
        var slowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser> fixture = new ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser>()
            .WithNotStartedActionGroup(slowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, slowActionGroup.Actions.First().Target.Node!.Name, slowActionGroup.Actions.Last().Target.Node!.Name, slowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.Node!.Name, fastActionGroup.Actions.Last().Target.Node!.Name, fastActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(fastActionGroup.CorrelationId);
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActionsOlderThanThreshold_SelectsAccordingToAge()
    {
        const double threshold = 2;
        var now = DateTime.UtcNow;
        var slowOldActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now.AddMinutes(-threshold - 1));
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now.AddMinutes(-threshold));
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser> fixture = new ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser>()
            .WithNotStartedActionGroup(slowOldActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, slowOldActionGroup.Actions.First().Target.Node!.Name, slowOldActionGroup.Actions.Last().Target.Node!.Name, slowOldActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.Node!.Name, fastActionGroup.Actions.Last().Target.Node!.Name, fastActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(slowOldActionGroup.CorrelationId);
    }
}