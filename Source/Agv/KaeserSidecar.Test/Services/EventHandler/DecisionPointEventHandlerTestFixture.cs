using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

/// <summary>
/// Provides a test fixture for <see cref="DecisionPointEventHandler"/>
/// </summary>
public class DecisionPointEventHandlerTestFixture: MassTransitConsumerTestFixture<DecisionPointEventHandler>
{
    private readonly Mock<ITargetSelector> _targetSelectorMock = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{DecisionPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<DecisionPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<DecisionPointEventHandler>>().Object);
        collection.AddTransient(_ => _targetSelectorMock.Object);
        return this;
    }

    /// <inheritdoc cref="DecisionPointEventHandlerTestFixture"/>>
    public DecisionPointEventHandlerTestFixture()
    {
        Build();
        _targetSelectorMock.Setup(x => x.Enqueue(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        _targetSelectorMock.Setup(x => x.SetStationCount(It.IsAny<int>())).Verifiable();
    }

    public void VerifyActionEnqueued( Guid action, Guid agv, Times times)
    {
        _targetSelectorMock.Verify(x => x.Enqueue(action, agv), times);
    }

    public void VerifyStationCountSet(int consideredStations, Times times)
    {
        _targetSelectorMock.Verify(x => x.SetStationCount(consideredStations), times);
    }
}