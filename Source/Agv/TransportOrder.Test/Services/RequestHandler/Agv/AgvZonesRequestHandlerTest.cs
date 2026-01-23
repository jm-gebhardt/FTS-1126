using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using RemoveAgvFromZone = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RemoveAgvFromZone;
using CommonRemoveAgvFromZone = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RemoveAgvFromZone;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvZonesRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_NoOnlineAgv_ThrowsAgvNotFoundException()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneName]);
        AgvRequestsHandlerTestFixture<RemoveAgvFromAllZones, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromAllZones, AgvZonesRequestHandler>()
            .WithAgv(agv)
            .WithOnlineAgv(null)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new RemoveAgvFromAllZones(agv.CorrelationId, "user")));
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_ValidData_PublishesRemoveAgvFromZone()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var offlineAgv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneName]);
        offlineAgv.GoOffline();
        var onlineAgv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<RemoveAgvFromAllZones, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromAllZones, AgvZonesRequestHandler>()
            .WithAgv(offlineAgv)
            .WithOnlineAgv(onlineAgv)
            .Build();
        
        await fixture.GetResponseAsync<Success>(new RemoveAgvFromAllZones(offlineAgv.CorrelationId, "user"));

        await fixture.ShouldHavePublishedAsync<CommonRemoveAgvFromZone>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromAllZones>();
        var commandToDevice = fixture.TestHarness.Published.Select<CommonRemoveAgvFromZone>().First().Context.Message;
        commandToDevice.AgvToRemove.ShouldBe(offlineAgv.SerialNumber);
        commandToDevice.CorrelationId.ShouldBe(onlineAgv.CorrelationId);
        commandToDevice.ZoneId.ShouldBe(zone.ZoneName);
        commandToDevice.SerialNumber.ShouldBe(onlineAgv.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZone_NoOnlineAgv_ThrowsAllAgvsOffline()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneName]);
        AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler>()
            .WithAgv(agv)
            .WithOnlineAgv(null)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AllAgvsOfflineException>(() => fixture.GetResponseAsync<Success>(new RemoveAgvFromZone(agv.CorrelationId, zone.ZoneName, "user")));
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZone_ValidData_PublishesRemoveAgvFromZone()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var offlineAgv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneName]);
        offlineAgv.GoOffline();
        var onlineAgv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler>()
            .WithAgv(offlineAgv)
            .WithOnlineAgv(onlineAgv)
            .Build();

        await fixture.GetResponseAsync<Success>(new RemoveAgvFromZone(offlineAgv.CorrelationId, zone.ZoneName, "user"));

        await fixture.ShouldHavePublishedAsync<CommonRemoveAgvFromZone>();
        var commandToDevice = fixture.TestHarness.Published.Select<CommonRemoveAgvFromZone>().First().Context.Message;
        commandToDevice.AgvToRemove.ShouldBe(offlineAgv.SerialNumber);
        commandToDevice.CorrelationId.ShouldBe(onlineAgv.CorrelationId);
        commandToDevice.ZoneId.ShouldBe(zone.ZoneName);
        commandToDevice.SerialNumber.ShouldBe(onlineAgv.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZone_AgvNotInZone_RespondsAgvNotInZone()
    {
        var offlineAgv = AgvGenerator.GenerateIdleAgv();
        offlineAgv.GoOffline();
        var onlineAgv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromZone, AgvZonesRequestHandler>()
            .WithAgv(offlineAgv)
            .WithOnlineAgv(onlineAgv)
            .Build();
        
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotInZoneException>( () => fixture.GetResponseAsync<Success>(new RemoveAgvFromZone(offlineAgv.CorrelationId, "test", "user")));
    }
}