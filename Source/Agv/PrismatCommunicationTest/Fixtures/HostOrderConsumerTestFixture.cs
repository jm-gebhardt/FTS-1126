using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

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
        _hostCommunicatorMock.Setup(x => x.ReportOrderCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderRepository(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.Add(It.IsAny<HostOrder>())).Verifiable();
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder!);
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public void VerifyHostOrderAdded(Times times = default)
    {
        HostOrderRepositoryMock.Verify(x => x.Add(It.IsAny<HostOrder>()), times);
    }

    public void VerifyReportOrderCompletion(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportOrderCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), times);
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}