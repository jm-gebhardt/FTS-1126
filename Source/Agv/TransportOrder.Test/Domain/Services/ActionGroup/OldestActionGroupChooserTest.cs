using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class OldestActionGroupChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActions_SelectsDependingOnAge()
    {
        var now = DateTime.UtcNow;
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now.AddMinutes(-1));
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        ChooseAgvActionGroupTestFixture<OldestActionGroupChooser> fixture = new ChooseAgvActionGroupTestFixture<OldestActionGroupChooser>()
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.Node!.Name, oldSlowActionGroup.Actions.Last().Target.Node!.Name, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.Node!.Name, fastActionGroup.Actions.Last().Target.Node!.Name, fastActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldSlowActionGroup.CorrelationId);
    }
}