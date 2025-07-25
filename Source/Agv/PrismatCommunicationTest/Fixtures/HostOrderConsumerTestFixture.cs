using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Semaphores;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class HostOrderConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>, IDisposable
    where TConsumer : class, IConsumer
{
    private readonly Mock<IHostCommunicator> _hostCommunicatorMock = new();
    public readonly Mock<IHostOrderRepository> HostOrderRepositoryMock = new();
    public readonly KaeserGraphConfiguration Configuration = OrderGenerator.KaeserConfiguration;
    private readonly Mock<IHostOrderValidationService> _validationService = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<ISystemStatusRepository> _systemStateRepositoryMock = new();
    private readonly SemaphoreProvider<IDoppelspielSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IDoppelspielSemaphore>>>().Object);

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton<ISemaphoreProvider<IDoppelspielSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => HostOrderRepositoryMock.Object);
        collection.AddTransient(_ => _validationService.Object);
        collection.AddTransient(_ => _hostCommunicatorMock.Object);
        collection.AddTransient(_ => _systemStateRepositoryMock.Object);
        collection.AddSingleton(_ => Configuration);
        collection.AddTransient(_ => _stationRepository.Object);
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

    public HostOrderConsumerTestFixture<TConsumer> WithStationGroup(bool? isOccupied, bool hasHostOrders, HostOrder? nextHostOrder = null)
    {
        var station = new Station();
        if (isOccupied != null && isOccupied.Value)
        {
            station.AddAgv(Guid.NewGuid());
        }
        var stationGroup = isOccupied == null ? null : new StationGroup([station]);
        _stationRepository.Setup(x => x.GetStationGroup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(stationGroup);
        HostOrderRepositoryMock.Setup(x => x.HasCurrentHostOrdersForStationGroup(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>())).ReturnsAsync(hasHostOrders);
        HostOrderRepositoryMock.Setup(x => x.GetNextHostOrderForStationGroup(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>())).ReturnsAsync(nextHostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithSystemStatus(SystemStatus? status)
    {
        _systemStateRepositoryMock.Setup(x => x.GetStatus()).ReturnsAsync(status);
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

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrders(ICollection<HostOrder> hostOrders)
    {
        var queue = new Queue<HostOrder>(hostOrders);
        HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() =>  queue.Dequeue());
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderWithSameHu(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrderByHu(It.IsAny<string>())).ReturnsAsync(hostOrder);
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrdersByHu(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(hostOrder== null ? [] : [hostOrder]);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithUnMatchedPickOrder(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextUnmatchedDoppelspielPick(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>(), It.IsAny<Guid>())).ReturnsAsync(hostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithValidationError((int ErrorCode, string ErrorInfo)? error)
    {
        _validationService.Setup(x => x.ValidateAsync(It.IsAny<HostOrder>())).ReturnsAsync(error);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(Guid? correlationId)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(It.IsAny<string>())).ReturnsAsync(correlationId);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextPickOrder(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextPickForSources(It.IsAny<ICollection<string>>())).ReturnsAsync(hostOrder);
        return this;
    }

    public void VerifyHostOrderAdded(Times times = default)
    {
        HostOrderRepositoryMock.Verify(x => x.Add(It.IsAny<HostOrder>()), times);
    }

    public void VerifyReportOrderCompletion(string? destination=null,Times times = default)
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

    public void VerifyDoppelspielIsNull(Times times)
    {
        HostOrderRepositoryMock.Verify(r => r.UpdateAsync(It.Is<HostOrder>(ho => ho.DoppelspielPartner == null)), times);
    }

    public void Dispose()
    {
        _hostCommunicatorMock.Invocations.Clear();
    }
}