using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.MaintenanceLog;

[TestClass]
public class MaintenanceLogRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_TriggerAddingMaintenanceLog_ValidData_PublishesAddMaintenanceLog()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        MaintenanceLogRequestHandlerTestFixture<TriggerAddingMaintenanceLog, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<TriggerAddingMaintenanceLog, MaintenanceLogRequestHandler>()
            .WithAgvs([agv]);

        await fixture.GetResponseAsync<Success>(new TriggerAddingMaintenanceLog("user", agv.CorrelationId, MaintenanceType.TiresReplaced, DateTime.UtcNow, "comment"));

        await fixture.ShouldHavePublishedAsync<AddMaintenanceLog>();
    }

    [TestMethod]
    public async Task Consume_TriggerAddingMaintenanceLog_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        MaintenanceLogRequestHandlerTestFixture<TriggerAddingMaintenanceLog, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<TriggerAddingMaintenanceLog, MaintenanceLogRequestHandler>()
            .WithAgvs([]);

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new TriggerAddingMaintenanceLog("user", agv.CorrelationId, MaintenanceType.TiresReplaced, DateTime.UtcNow, "comment")));
    }

    [TestMethod]
    public async Task Consume_GetMaintenanceLogForAgv_ValidData_PublishesGotMaintenanceLogs()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog("user", agv.CorrelationId, DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment");
        MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgv, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgv, MaintenanceLogRequestHandler>()
            .WithAgvs([agv])
            .WithMaintenanceLogs([maintenanceLog]);

        Response<GotMaintenanceLogs> response = await fixture.GetResponseAsync<GotMaintenanceLogs>(new GetMaintenanceLogsForAgv(agv.CorrelationId, maintenanceLog.MaintenanceType));

        response.Message.MaintenanceLogs.Single().ShouldBe(maintenanceLog);
    }

    [TestMethod]
    public async Task Consume_GetMaintenanceLogsForAgv_UnknownAgv_ThrowsAgvNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog("user", agv.CorrelationId, DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment");
        MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgv, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgv, MaintenanceLogRequestHandler>()
            .WithAgvs([])
            .WithMaintenanceLogs([maintenanceLog]);

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotMaintenanceLogs>(new GetMaintenanceLogsForAgv(agv.CorrelationId, maintenanceLog.MaintenanceType)));
    }

    [TestMethod]
    public async Task Consume_GetMaintenanceLogsForAgvs_ValidData_PublishesGotMaintenanceLogs()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog("user", agv.CorrelationId, DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment");
        MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgvs, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgvs, MaintenanceLogRequestHandler>()
            .WithAgvs([agv])
            .WithMaintenanceLogs([maintenanceLog]);

        Response<GotMaintenanceLogs> response = await fixture.GetResponseAsync<GotMaintenanceLogs>(new GetMaintenanceLogsForAgvs([agv.CorrelationId], maintenanceLog.MaintenanceType));

        response.Message.MaintenanceLogs.Single().ShouldBe(maintenanceLog);
    }

    [TestMethod]
    public async Task Consume_GetMaintenanceLogsForAgvs_UnknownAgv_ThrowsAgvsNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var maintenanceLog = new TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog("user", agv.CorrelationId, DateTime.UtcNow, MaintenanceType.TiresReplaced, "comment");
        MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgvs, MaintenanceLogRequestHandler> fixture = new MaintenanceLogRequestHandlerTestFixture<GetMaintenanceLogsForAgvs, MaintenanceLogRequestHandler>()
            .WithAgvs([])
            .WithMaintenanceLogs([maintenanceLog]);

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotMaintenanceLogs>(new GetMaintenanceLogsForAgvs([agv.CorrelationId], maintenanceLog.MaintenanceType)));
    }
}