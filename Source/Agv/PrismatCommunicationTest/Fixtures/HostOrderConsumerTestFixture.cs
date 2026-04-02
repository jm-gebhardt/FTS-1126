using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class HostOrderConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>, IDisposable
    where TConsumer : class, IConsumer
{
    private readonly Mock<IHostCommunicator> _hostCommunicatorMock = new();
    public readonly Mock<IHostOrderRepository> HostOrderRepositoryMock = new();

    [PublicAPI]
    public readonly CustomerConfiguration Configuration = OrderGenerator.CustomerConfiguration;
   
    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => HostOrderRepositoryMock.Object);
        collection.AddTransient(_ => _hostCommunicatorMock.Object);
        collection.AddSingleton(_ => Configuration);
        return this;
    }

    public HostOrderConsumerTestFixture()
    {
        Build();
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostCommunicator()
    {
        _hostCommunicatorMock.Setup(x => x.ReportOrderCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.ReportPickCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderRepository(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<HostOrder>())).Verifiable();
        HostOrderRepositoryMock.Setup(x => x.Add(It.IsAny<HostOrder>())).Verifiable();
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder!);
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrders(ICollection<HostOrder> hostOrders)
    {
        var queue = new Queue<HostOrder>(hostOrders);
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(queue.Dequeue);
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderWithSameHu(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrderByHu(It.IsAny<string>())).ReturnsAsync(hostOrder);
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrdersByHu(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(hostOrder == null ? [] : [hostOrder]);
        return this;
    }

    public void VerifyReportOrderCompletion(string? destination = null, Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportOrderCompletion(It.IsAny<string>(), It.IsAny<string>(), destination ?? It.IsAny<string>()), times);
    }

    public void VerifyPickCompletion(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportPickCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), times);
    }

    public void VerifyReportOrderError(string hu, string source, string destination, int errorCode, Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportOrderError(hu, source, destination, errorCode, It.IsAny<string>(), It.IsAny<DateTime>()), times);
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}