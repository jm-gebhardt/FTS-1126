using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class HostOrderConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>, IDisposable
    where TConsumer : class, IConsumer
{
    private readonly Mock<IHostCommunicator> _hostCommunicatorMock = new();
    public readonly Mock<IHostOrderRepository> HostOrderRepositoryMock = new();
    public readonly KaeserGraphConfiguration Configuration = OrderGenerator.KaeserConfiguration;

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
        HostOrderRepositoryMock.Setup(x => x.Add(It.IsAny<HostOrder>())).Verifiable();
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder!);
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithDropOrdersHeadingTowardsDecisionPoint(bool hasHostOrders)
    {
        HostOrderRepositoryMock.Setup(x => x.HasDropOrdersHeadingTowardsDecisionPoint()).ReturnsAsync(hasHostOrders);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithDropOrdersHeadingTowardsCargoTriggerPoint(bool hasHostOrders)
    {
        HostOrderRepositoryMock.Setup(x => x.HasDropOrdersHeadingTowardsCargoTriggerPoint()).ReturnsAsync(hasHostOrders);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithDropOrdersHeadingTowardsShortCircleTriggerPoint(bool hasHostOrders)
    {
        HostOrderRepositoryMock.Setup(x => x.HasDropOrdersHeadingTowardsShortCircleTriggerPoint()).ReturnsAsync(hasHostOrders);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextIncompleteOrderWithUnknownDropTarget(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextIncompleteOrderWithUnknownDropTarget()).ReturnsAsync(hostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextIncompleteOrderWithUnknownPickTarget(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextIncompleteOrderWithUnknownPickTarget()).ReturnsAsync(hostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextPickOrderForShortCircleDualOperation(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextPickOrderForShortCircleDualOperation()).ReturnsAsync(hostOrder);
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

    public void VerifyPickCompletion(Times times = default)
    {
        _hostCommunicatorMock.Verify(x => x.ReportPickCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), times);
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}