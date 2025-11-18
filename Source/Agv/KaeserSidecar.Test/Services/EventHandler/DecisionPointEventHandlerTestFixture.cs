using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderGenerator = Gebhardt.Acc.PrismatCommunicationTest.Helper.OrderGenerator;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

/// <summary>
/// Provides a test fixture for <see cref="DecisionPointEventHandler"/>
/// </summary>
public class DecisionPointEventHandlerTestFixture : MassTransitConsumerTestFixture<DecisionPointEventHandler>
{
    private readonly Mock<ITargetSelector> _targetSelectorMock = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{DecisionPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<DecisionPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<DecisionPointEventHandler>>().Object);
        collection.AddTransient(_ => _targetSelectorMock.Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        return this;
    }

    /// <inheritdoc cref="DecisionPointEventHandlerTestFixture"/>>
    public DecisionPointEventHandlerTestFixture()
    {
        Build();
        _targetSelectorMock.Setup(x => x.Enqueue(It.IsAny<Guid>(), It.IsAny<Guid>())).Verifiable();
        _targetSelectorMock.Setup(x => x.SetStationCount(It.IsAny<ICollection<Guid>>())).Verifiable();
    }

    public void VerifyActionEnqueued(Guid action, Guid agv, Times times)
    {
        _targetSelectorMock.Verify(x => x.Enqueue(action, agv), times);
    }

    public void VerifyNoActionEnqueued()
    {
        _targetSelectorMock.Verify(x => x.Enqueue(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    public void VerifyStationCountSet(ICollection<Guid> consideredStations, Times times)
    {
        _targetSelectorMock.Verify(x => x.SetStationCount(consideredStations), times);
    }

    public void VerifyUseJamStateSet(bool useJamState, Times times)
    {
        _targetSelectorMock.Verify(x => x.SetUseJamState(useJamState), times);
    }
}