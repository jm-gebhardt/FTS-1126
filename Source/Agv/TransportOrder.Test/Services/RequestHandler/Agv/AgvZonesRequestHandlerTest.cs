using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using RemoveAgvFromZones = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RemoveAgvFromZones;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvZonesRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_NoOnlineAgv_ThrowsAgvNotFoundException()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneId]);
        AgvRequestsHandlerTestFixture<RemoveAgvFromZones, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromZones, AgvZonesRequestHandler>()
            .WithAgv(agv)
            .WithOnlineAgv(null)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<Success>(new RemoveAgvFromZones(agv.CorrelationId)));
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_ValidData_PublishesRemoveAgvFromZone()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var offlineAgv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneId]);
        offlineAgv.GoOffline();
        var onlineAgv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<RemoveAgvFromZones, AgvZonesRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RemoveAgvFromZones, AgvZonesRequestHandler>()
            .WithAgv(offlineAgv)
            .WithOnlineAgv(onlineAgv)
            .Build();
        
        await fixture.GetResponseAsync<Success>(new RemoveAgvFromZones(offlineAgv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<RemoveAgvFromZone>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromZones>();
        var commandToDevice = fixture.TestHarness.Published.Select<RemoveAgvFromZone>().First().Context.Message;
        commandToDevice.AgvToRemove.ShouldBe(offlineAgv.SerialNumber);
        commandToDevice.CorrelationId.ShouldBe(onlineAgv.CorrelationId);
        commandToDevice.ZoneId.ShouldBe(zone.ZoneId);
        commandToDevice.SerialNumber.ShouldBe(onlineAgv.SerialNumber);
    }
}