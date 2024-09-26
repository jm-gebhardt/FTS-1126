using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class OldestActionGroupChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActions_SelectsDependingOnAge()
    {
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<OldestActionGroupChooser> fixture = new ChooseAgvActionGroupTestFixture<OldestActionGroupChooser>()
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.NodeId!, oldSlowActionGroup.Actions.Last().Target.NodeId!, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.NodeId!, fastActionGroup.Actions.Last().Target.NodeId!, fastActionGroup, 1f)
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldSlowActionGroup.CorrelationId);
    }
}