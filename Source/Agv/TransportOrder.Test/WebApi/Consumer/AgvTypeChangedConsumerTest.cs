using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvTypeChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_AgvTypeAcknowledged_ValidData_SendsAgvTypeAcknowledged()
    {
        var correlationId = Guid.NewGuid();
        var fixture = new ConsumerHubTestFixture<AgvTypeChangedConsumer, IAgvTypePublisher>();

        await fixture.PublishMessageAsync(new AgvTypeAcknowledged(correlationId));

        fixture.PublisherMock.Verify(h => h.SendAgvTypeAcknowledged(correlationId), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvTypeAdded_ValidData_SendsAgvTypeCreated()
    {
        var agvType = new AgvType("manufacturer-series", "manufacturer", "series", 1.5, 0.8, acknowledged: false, reportedByAgv: true, rawData: "");
        var typeInfo = AgvTypeGenerator.GenerateAgvTypeInfo(agvType.AgvTypeIdentifier, agvType.Manufacturer, agvType.SeriesName, agvType.Length, agvType.Width, agvType.Acknowledged, agvType.ReportedByAgv);
        var fixture = new ConsumerHubTestFixture<AgvTypeChangedConsumer, IAgvTypePublisher>();

        await fixture.PublishMessageAsync(new AgvTypeAdded(agvType.Id, typeInfo));

        var mapper = MappingHelper.GetMapper(new AgvTypeProfile());
        var agvTypeDto = mapper.Map<AgvTypeDto>(typeInfo) with { Id = agvType.Id };

        fixture.PublisherMock.Verify(h => h.SendAgvTypeCreated(agvTypeDto), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvTypeUpdated_ValidData_SendsAgvTypeUpdated()
    {
        var agvType = new AgvType("manufacturer-series", "manufacturer", "series", 1.5, 0.8, acknowledged: true, reportedByAgv: false, rawData: "");
        var typeInfo = AgvTypeGenerator.GenerateAgvTypeInfo(agvType.AgvTypeIdentifier, agvType.Manufacturer, agvType.SeriesName, agvType.Length, agvType.Width, agvType.Acknowledged, agvType.ReportedByAgv);
        var fixture = new ConsumerHubTestFixture<AgvTypeChangedConsumer, IAgvTypePublisher>();

        await fixture.PublishMessageAsync(new AgvTypeUpdated(agvType.Id, typeInfo));

        var mapper = MappingHelper.GetMapper(new AgvTypeProfile());
        var agvTypeDto = mapper.Map<AgvTypeDto>(typeInfo) with { Id = agvType.Id };

        fixture.PublisherMock.Verify(h => h.SendAgvTypeUpdated(agvTypeDto), Times.Once);
    }
}