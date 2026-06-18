using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

[TestClass]
public class GraphLayoutStoredHandlerTest
{
    [TestMethod]
    public async Task Consume_MatchingAgvs_PublishesPublishAgvGraphForEach()
    {
        var agv1 = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv1");
        var agv2 = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv2");
        var fixture = new GraphLayoutStoredHandlerTestFixture()
            .SetAgvs(agv1, agv2);

        await fixture.TestHarness.Bus.Publish(new LayoutStored(["L1"], ["manufacturer.series"]));
        await fixture.ShouldHaveConsumedAsync<LayoutStored>();

        ICollection<PublishAgvGraph> messages = fixture.GetPublishedMessages<PublishAgvGraph>();
        messages.Count.ShouldBe(2);
        messages.ShouldContain(m => m.AgvCorrelationId == agv1.CorrelationId);
        messages.ShouldContain(m => m.AgvCorrelationId == agv2.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_NoMatchingAgvs_DoesNotPublish()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new GraphLayoutStoredHandlerTestFixture()
            .SetAgvs(agv);

        await fixture.TestHarness.Bus.Publish(new LayoutStored(["L1"], ["Other.Type"]));
        await fixture.ShouldHaveConsumedAsync<LayoutStored>();

        await fixture.ShouldNotHavePublishedAsync<PublishAgvGraph>();
    }

    [TestMethod]
    public async Task Consume_MultipleVehicleTypes_OnlyPublishesForMatched()
    {
        var matchingAgv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "match");
        var fixture = new GraphLayoutStoredHandlerTestFixture()
            .SetAgvs(matchingAgv);

        await fixture.TestHarness.Bus.Publish(new LayoutStored(["L1"], ["manufacturer.series", "Unknown.Type"]));
        await fixture.ShouldHaveConsumedAsync<LayoutStored>();

        ICollection<PublishAgvGraph> messages = fixture.GetPublishedMessages<PublishAgvGraph>();
        messages.Count.ShouldBe(1);
        messages.Single().AgvCorrelationId.ShouldBe(matchingAgv.CorrelationId);
    }
}