using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class AlternatingModeChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActions_AlternatesMode()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<AlternatingModeChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.Node!.Name, actionGroup2.Actions.Last().Target.Node!.Name, actionGroup2, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        fixture.Service.FastestRouteMode.ShouldBeTrue();
        await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        fixture.Service.FastestRouteMode.ShouldBeFalse();
        await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        fixture.Service.FastestRouteMode.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActions_SelectsDependingOnMode()
    {
        var now = DateTime.UtcNow;
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name, createdDate: now.AddMinutes(-1));
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: now);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<AlternatingModeChooser>()
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.Node!.Name, oldSlowActionGroup.Actions.Last().Target.Node!.Name, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.Node!.Name, fastActionGroup.Actions.Last().Target.Node!.Name, fastActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        fixture.Service.FastestRouteMode.ShouldBeTrue();
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(fastActionGroup.CorrelationId);
        chosenGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        fixture.Service.FastestRouteMode.ShouldBeFalse();
        chosenGroup.ShouldNotBeNull();
        chosenGroup.CorrelationId.ShouldBe(oldSlowActionGroup.CorrelationId);
    }
}