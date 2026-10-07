using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using MassTransit;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler.FactsheetQueryHandlerTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

[TestClass]
public class FactsheetQueryHandlerTest
{
    [TestMethod]
    public async Task Consume_GetFactsheet_UploadedAgvType_RespondsGotFactsheetWithRawJson()
    {
        var fixture = new Fixture().WithAgvType(FactsheetJsonGenerator.FactsheetJson).Build();

        Response<GotFactsheet, FactsheetNotFound> response = await fixture.GetResponseAsync();

        response.Is(out Response<GotFactsheet>? gotFactsheet).ShouldBeTrue();
        gotFactsheet.ShouldNotBeNull().Message.FactsheetJson.ShouldBe(FactsheetJsonGenerator.FactsheetJson);
        gotFactsheet.Message.AgvTypeIdentifier.ShouldBe(FactsheetJsonGenerator.AgvTypeIdentifier);
    }

    [TestMethod]
    public async Task Consume_GetFactsheet_UnknownAgvType_RespondsFactsheetNotFound()
    {
        var fixture = new Fixture().Build();

        Response<GotFactsheet, FactsheetNotFound> response = await fixture.GetResponseAsync();

        response.Is(out Response<FactsheetNotFound>? notFound).ShouldBeTrue();
        notFound.ShouldNotBeNull().Message.AgvTypeIdentifier.ShouldBe(FactsheetJsonGenerator.AgvTypeIdentifier);
    }

    [TestMethod]
    public async Task Consume_GetFactsheet_AgvTypeWithoutRawData_RespondsFactsheetNotFound()
    {
        var fixture = new Fixture().WithAgvType(string.Empty).Build();

        Response<GotFactsheet, FactsheetNotFound> response = await fixture.GetResponseAsync();

        response.Is(out Response<FactsheetNotFound>? _).ShouldBeTrue();
    }
}