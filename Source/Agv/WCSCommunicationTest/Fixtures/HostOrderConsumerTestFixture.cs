using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.WCSCommunication.Domain.Repositories;
using Gebhardt.Acc.WCSCommunication.Domain.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.WCSCommunicationTest.Fixtures;

public class HostOrderConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>, IDisposable
    where TConsumer : class, IConsumer
{
    public readonly Mock<IHostOrderRepository> HostOrderRepositoryMock = new();

    public readonly Mock<IHostCommunicator> HostCommunicatorMock = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => HostOrderRepositoryMock.Object);
        collection.AddTransient(_ => HostCommunicatorMock.Object);
        return this;
    }

    public HostOrderConsumerTestFixture()
    {
        Build();
        HostCommunicatorMock.Setup(x => x.TransmitHostOrderStarted(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        HostCommunicatorMock.Setup(x => x.TransmitHostOrderCancellationFailure(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        HostCommunicatorMock.Setup(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        HostCommunicatorMock.Setup(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>())).Verifiable();
        HostCommunicatorMock.Setup(x => x.TransmitHostOrderHandlingSucceeded(It.IsAny<string>())).Verifiable();
    }

    public void Dispose()
    {
        HostCommunicatorMock.Invocations.Clear();
    }
}