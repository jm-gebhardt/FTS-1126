using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Domain.Repositories;
using Gebhardt.Acc.WCSCommunication.Domain.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.WCSCommunicationTest.Fixtures;

public class HostOrderConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>, IDisposable
    where TConsumer : class, IConsumer
{
    private readonly Mock<IHostCommunicator> _hostCommunicatorMock = new();
    public readonly Mock<IHostOrderRepository> HostOrderRepositoryMock = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => HostOrderRepositoryMock.Object);
        collection.AddTransient(_ => _hostCommunicatorMock.Object);
        return this;
    }

    public HostOrderConsumerTestFixture()
    {
        Build();
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostCommunicator()
    {
        _hostCommunicatorMock.Setup(x => x.TransmitHostOrderStarted(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.TransmitHostOrderCancellationFailure(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.TransmitHostOrderHandlingSucceeded(It.IsAny<string>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderRepository(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<HostOrder>())).Verifiable();
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder!);
        HostOrderRepositoryMock.Setup(x => x.RemoveAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public void VerifyHostOrderAdded(Times times = default)
    {
        HostOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<HostOrder>()), times);
    }

    public void VerifyHostOrderRemoved(Times times = default)
    {
        HostOrderRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<HostOrder>()), times);
    }

    public void VerifyHostOrderHandlingFailure(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>()), times);
    }

    public void VerifyHostOrderHandlingSucceeded(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingSucceeded(It.IsAny<string>()), times);
    }

    public void VerifyHostOrderCancellationFailure(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationFailure(It.IsAny<string>(), It.IsAny<string>()), times);
    }

    public void VerifyHostOrderCancellationSucceeded(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>()), times);
    }

    public void VerifyHostOrderStarted(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.TransmitHostOrderStarted(It.IsAny<string>(), It.IsAny<string>()), times);
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}