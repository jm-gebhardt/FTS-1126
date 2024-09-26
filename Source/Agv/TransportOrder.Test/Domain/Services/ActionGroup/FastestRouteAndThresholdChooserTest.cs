using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class FastestRouteAndThresholdChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActionsYoungerThanThreshold_SelectsAccordingToCosts()
    {
        var slowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser> fixture = new ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser>()
            .WithNotStartedActionGroup(slowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, slowActionGroup.Actions.First().Target.NodeId!, slowActionGroup.Actions.Last().Target.NodeId!, slowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.NodeId!, fastActionGroup.Actions.Last().Target.NodeId!, fastActionGroup, 1f)
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
        var slowOldActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id, createdDate: now.AddMinutes(-threshold - 1));
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate: now.AddMinutes(-threshold));
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser> fixture = new ChooseAgvActionGroupTestFixture<FastestRouteAndThresholdChooser>()
            .WithNotStartedActionGroup(slowOldActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, slowOldActionGroup.Actions.First().Target.NodeId!, slowOldActionGroup.Actions.Last().Target.NodeId!, slowOldActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.NodeId!, fastActionGroup.Actions.Last().Target.NodeId!, fastActionGroup, 1f)
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(slowOldActionGroup.CorrelationId);
    }
}