using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class AlternatingModeChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AutomaticActions_AlternatesMode()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); 
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<AlternatingModeChooser> fixture = new ChooseAgvActionGroupTestFixture<AlternatingModeChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.NodeId!, actionGroup1.Actions.Last().Target.NodeId!, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.NodeId!, actionGroup2.Actions.Last().Target.NodeId!, actionGroup2, 1f)
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
        var oldSlowActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id, createdDate: now.AddMinutes(-1));
        var fastActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate: now);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        ChooseAgvActionGroupTestFixture<AlternatingModeChooser> fixture = new ChooseAgvActionGroupTestFixture<AlternatingModeChooser>()
            .WithNotStartedActionGroup(oldSlowActionGroup)
            .WithNotStartedActionGroup(fastActionGroup)
            .WithAgvHasRouteToDestination(agv, oldSlowActionGroup.Actions.First().Target.NodeId!, oldSlowActionGroup.Actions.Last().Target.NodeId!, oldSlowActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, fastActionGroup.Actions.First().Target.NodeId!, fastActionGroup.Actions.Last().Target.NodeId!, fastActionGroup, 1f)
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