using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.OrderTranslator.Messages.Stations;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

[TestClass]
public class StationEventHandlerTest
{
    [TestMethod]
    public async Task Consume_StationConfigurationUpdateRequested_PublishesReloadStationConfiguration()
    {
        var fixture = new StationEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new StationConfigurationUpdateRequested());

        await fixture.ShouldHavePublishedAsync<ReloadStationConfiguration>();
    }
}
