using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Trigger;

public class TriggerRequestHandlerTestFixture<TMessage> : MediatorTestFixture<TMessage, TriggerRequestHandler, MockConsumer>
    where TMessage : class
{
    private readonly Mock<ITriggerRepository> _triggerRepository = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<IMapRepository> _mapRepository = new();

    private class MockConsumer(Location? loc, string? message) : IConsumer<AddTriggerLocation>
    {
        public async Task Consume(ConsumeContext<AddTriggerLocation> context)
        {
            await context.RespondAsync(new TriggerLocationAdded(loc, message));
        }
    }

    private MockConsumer ConsumerMock { get; set; } = null!;

    public TriggerRequestHandlerTestFixture()
    {
        _graphRepository.Setup(x => x.ThrowIfNodeNotExistsAsync(It.IsAny<Guid?>())).ThrowsAsync(new NodeNotFoundException(nodeId: null));
    }

    public new TriggerRequestHandlerTestFixture<TMessage> Build()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _triggerRepository.Object);
            s.AddTransient(_ => _graphRepository.Object);
            s.AddTransient(_ => _mapRepository.Object);
        });
        base.Build();
        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithLocationResponse(Location? triggerLocation = null, string? exception = null)
    {
        ConsumerMock = new MockConsumer(triggerLocation, exception);
        AddServices(s => s.AddTransient(_ => ConsumerMock));
        AddConsumers<MockConsumer>();

        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithTriggers(ICollection<TransportOrder.Domain.Models.Trigger.Trigger> triggers)
    {
        _triggerRepository.Setup(x => x.GetTriggers()).ReturnsAsync(triggers);
        _triggerRepository.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((Guid id, bool _) =>
        {
            var trigger = triggers.SingleOrDefault(t => t.Id == id);
            return trigger ?? throw new TriggerNotFoundException(id);
        });
        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithDeletedTriggers(ICollection<TransportOrder.Domain.Models.Trigger.Trigger> deletedTriggers)
    {
        _triggerRepository.Setup(x => x.GetDeletedTriggers()).ReturnsAsync(deletedTriggers);
        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithMapExists(bool exists)
    {
        _mapRepository.Setup(x => x.Exists(It.IsAny<Guid>())).Returns(exists);
        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithGraphElementExists(bool exists)
    {
        _graphRepository.Setup(x => x.EdgeExistsAsync(It.IsAny<Guid>())).ReturnsAsync(exists);
        _graphRepository.Setup(x => x.NodeExistsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(exists);
        if (exists)
        {
            _graphRepository.Setup(x => x.ThrowIfNodeNotExistsAsync(It.IsAny<Guid?>()));
        }
        return this;
    }

    public TriggerRequestHandlerTestFixture<TMessage> WithNode(GraphNode node)
    {
        _graphRepository.Setup(x => x.GetNodeAsync(node.Id)).ReturnsAsync(node);
        _graphRepository.Setup(x => x.NodeExistsByIdAsync(node.Id)).ReturnsAsync(true);
        return this;
    }
}