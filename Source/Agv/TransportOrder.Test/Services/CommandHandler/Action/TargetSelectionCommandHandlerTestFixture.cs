using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

/// <summary>
/// Provides a test fixture for <see cref="TargetSelectionCommandHandler"/>
/// </summary>
public class TargetSelectionCommandHandlerTestFixture : MassTransitConsumerTestFixture<TargetSelectionCommandHandler>
{
    private readonly Mock<ITargetSelector> _targetSelectorMock = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{TargetSelectionCommandHandler}"/>>
    protected override MassTransitConsumerTestFixture<TargetSelectionCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<TargetSelectionCommandHandler>>().Object);
        collection.AddTransient(_ => _targetSelectorMock.Object);
       // collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        return this;
    }

    /// <inheritdoc cref="TargetSelectionCommandHandlerTestFixture"/>>
    public TargetSelectionCommandHandlerTestFixture()
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