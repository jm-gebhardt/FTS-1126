using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvModeRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_SetMaintenanceMode_ValidData_PublishesSetMaintenanceModeRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<SetMaintenanceMode, AgvModeRequestHandler > fixture = new AgvRequestsHandlerTestFixture<SetMaintenanceMode, AgvModeRequestHandler>()
            .WithAgv(agv)
            .Build();
        await fixture.GetResponseAsync<Success>(new SetMaintenanceMode(agv.CorrelationId, "user", MaintenanceReason.Other, "reason description", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, false));

        await fixture.ShouldHavePublishedAsync<SetMaintenanceMode>();
    }

    [TestMethod]
    public async Task Consume_SetUserMode_ValidData_PublishesSetUserModeRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<SetUserMode, AgvModeRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SetUserMode, AgvModeRequestHandler>()
            .WithAgv(agv)
            .Build();
        await fixture.GetResponseAsync<Success>(new SetUserMode(agv.CorrelationId, "user", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, false));
        await fixture.ShouldHavePublishedAsync<SetUserMode>();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_NoPausedActionGroup_PublishesSetNormalModeRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler>()
            .WithAgv(agv)
            .Build();
        await fixture.GetResponseAsync<Success>(new SetNormalMode(agv.CorrelationId, "user"));
        await fixture.ShouldHavePublishedAsync<SetNormalMode>();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_PausedActionGroup_PublishesResumeActionGroupRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Paused, assignedAgv: agv.CorrelationId);
        AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler>()
            .WithAgv(agv, actionGroup)
            .Build();
        await fixture.GetResponseAsync<Success>(new SetNormalMode(agv.CorrelationId, "user"));
        await fixture.ShouldHavePublishedAsync<ResumeActionGroup>();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_LockedAgv_PublishesUnlockAgvRespondsSuccess()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler> fixture = new AgvRequestsHandlerTestFixture<SetNormalMode, AgvModeRequestHandler>()
            .WithAgv(agv)
            .Build();
        await fixture.GetResponseAsync<Success>(new SetNormalMode(agv.CorrelationId, "user"));
        await fixture.ShouldHavePublishedAsync<UnlockAgv>();
    }
}