using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.FactsheetEventHandlerTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class FactsheetEventHandlerTest
{
    [TestMethod]
    public async Task Consume_FactsheetUploaded_ValidData_PublishesCacheFactsheet()
    {
        var fixture = new Fixture();

        var command = await fixture.PublishAndGetFirstMessageAsync<FactsheetUploaded, CacheFactsheet>(
            new FactsheetUploaded(FactsheetJsonGenerator.AgvTypeId, FactsheetJsonGenerator.AgvTypeIdentifier, FactsheetJsonGenerator.FactsheetJson));

        command.CorrelationId.ShouldBe(FactsheetJsonGenerator.AgvTypeId);
        command.AgvTypeIdentifier.ShouldBe(FactsheetJsonGenerator.AgvTypeIdentifier);
        command.FactsheetJson.ShouldBe(FactsheetJsonGenerator.FactsheetJson);
    }
}