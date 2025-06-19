using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

/// <summary>
/// Provides a test fixture for <see cref="ShortLoopWaitingTriggerPointEventHandler"/>
/// </summary>
public class ShortLoopWaitingPointEventHandlerTestFixture : MassTransitConsumerTestFixture<ShortLoopWaitingTriggerPointEventHandler>
{
    private readonly Mock<IWaitingHandler> _waitingHandlerMock = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{ShortLoopWaitingTriggerPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<ShortLoopWaitingTriggerPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<ShortLoopWaitingTriggerPointEventHandler>>().Object);
        collection.AddTransient(_ => _waitingHandlerMock.Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        return this;
    }

    /// <inheritdoc cref="MassTransitConsumerTestFixture{ShortLoopWaitingTriggerPointEventHandler}"/>>
    public ShortLoopWaitingPointEventHandlerTestFixture()
    {
        Build();
        _waitingHandlerMock.Setup(x => x.StartHandling(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>())).Verifiable();
        _waitingHandlerMock.Setup(x => x.StopHandling(It.IsAny<Guid>())).Verifiable();
    }

    public void VerifyActionHandled(Guid action, Guid agv, DateTime createdDate, Times times)
    {
        _waitingHandlerMock.Verify(x => x.StartHandling(action, agv, createdDate), times);
    }

    public void VerifyNoActionHandled()
    {
        _waitingHandlerMock.Verify(x => x.StartHandling(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    public void VerifyActionHandingStopped(Guid action, Times times)
    {
        _waitingHandlerMock.Verify(x => x.StopHandling(action), times);
    }

    public void VerifyActionHandingNotStopped()
    {
        _waitingHandlerMock.Verify(x => x.StopHandling(It.IsAny<Guid>()), Times.Never);
    }
}