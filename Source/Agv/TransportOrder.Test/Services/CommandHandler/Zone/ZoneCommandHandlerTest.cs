using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Zone;

[TestClass]
public class ZoneCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_AddZone_ValidData_ZoneAdded()
    {
        var fixture = new ZoneCommandHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new AddZone(new AgvZone("zoneId", 1, [new PolygonPoint(1, 1)], [])));

        await fixture.ShouldHaveConsumedAsync<AddZone>();
        fixture.VerifyZoneAdded();
    }
}