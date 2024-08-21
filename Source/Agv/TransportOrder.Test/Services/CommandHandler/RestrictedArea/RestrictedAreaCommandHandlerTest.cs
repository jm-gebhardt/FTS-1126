using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.RestrictedArea;

[TestClass]
public class RestrictedAreaCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_SaveRestrictedArea_ValidData_RestrictedAreaSaved()
    {
        var fixture = new RestrictedAreaCommandHandlerTestFixture();

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(polygon);

        await fixture.PublishMessageAsync(new SaveRestrictedArea(restrictedArea));

        fixture.VerifyRestrictedAreaAdded();
    }

    [TestMethod]
    public async Task Consume_UpdateRestrictedArea_ValidData_RestrictedAreaUpdated()
    {
        var fixture = new RestrictedAreaCommandHandlerTestFixture();

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(polygon);

        await fixture.PublishMessageAsync(new UpdateRestrictedArea(restrictedArea));

        fixture.VerifyRestrictedAreaUpdated();
    }

    [TestMethod]
    public async Task Consume_DeleteRestrictedArea_ValidData_RestrictedAreaDeleted()
    {
        var fixture = new RestrictedAreaCommandHandlerTestFixture();

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(polygon);

        await fixture.PublishMessageAsync(new DeleteRestrictedArea(restrictedArea.Id));

        fixture.VerifyRestrictedAreaDeleted(restrictedArea.Id);
    }
}