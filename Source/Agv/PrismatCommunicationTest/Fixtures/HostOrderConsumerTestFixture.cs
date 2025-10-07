using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Semaphores;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using JetBrains.Annotations;
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
    private readonly SemaphoreProvider<IDoppelspielSemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IDoppelspielSemaphore>>>().Object);
    private readonly Mock<IAklAgvLimitGuard> _aklAgvLimitGuard = new();
    private HostOrder? _nextHostOrder;
    private StationGroup? _stationGroup;

    // Disable, as only set when HostOrderCommandHandler is tested
    // ReSharper disable once StaticMemberInGenericType
    private static int? _quantity;

    [UsedImplicitly]
    private class MockRequestConsumer : IConsumer<GetQueueFillQuantities>
    {
        public async Task Consume(ConsumeContext<GetQueueFillQuantities> context)
        {
            if (_quantity == null)
            {
                await context.RespondAsync(new GotQueueFillQuantities([]));
                return;
            }
            await context.RespondAsync(new GotQueueFillQuantities([new QueueFillQuantity(OrderGenerator.KaeserConfiguration.AplQueueName, 2, 2, _quantity.Value)]));
        }
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<MockRequestConsumer>();
    }

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton<ISemaphoreProvider<IDoppelspielSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => HostOrderRepositoryMock.Object);
        collection.AddTransient(_ => _validationService.Object);
        collection.AddTransient(_ => _hostCommunicatorMock.Object);
        collection.AddSingleton(_ => Configuration);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _aklAgvLimitGuard.Object);
        return this;
    }

    public HostOrderConsumerTestFixture(int? enteredAgvs=null)
    {
        _quantity = enteredAgvs;
        _stationRepository.Setup(x => x.GetStationGroup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => _stationGroup);
        HostOrderRepositoryMock.Setup(x => x.GetNextHostOrderForStationGroup(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>())).ReturnsAsync(() => _nextHostOrder);

        Build();
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostCommunicator()
    {
        _hostCommunicatorMock.Setup(x => x.ReportOrderCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _hostCommunicatorMock.Setup(x => x.ReportPickCompletion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextHostOrder(HostOrder nextHostOrder)
    {
        _nextHostOrder = nextHostOrder;
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithinAklAgvLimit(bool isWithinLimit)
    {
        if (isWithinLimit)
        {
            var station = new Station();
            station.AddAgv(Guid.NewGuid());
            _stationGroup = new StationGroup([station]);
        }
        _aklAgvLimitGuard.Setup(x => x.IsWithinAklAgvLimitAsync(It.IsAny<HostOrder>())).ReturnsAsync(isWithinLimit);
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
            .ReturnsAsync(() => queue.Dequeue());
        HostOrderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<HostOrder>())).Verifiable();
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithCurrentOrderForAgv(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetCurrentHostOrderForAgv(It.IsAny<Guid>())).ReturnsAsync(hostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithHostOrderWithSameHu(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrderByHu(It.IsAny<string>())).ReturnsAsync(hostOrder);
        HostOrderRepositoryMock.Setup(x => x.GetLatestUnfinishedOrdersByHu(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(hostOrder == null ? [] : [hostOrder]);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithUnmatchedPickOrder(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextUnmatchedDoppelspielPick(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>(), It.IsAny<Guid?>())).ReturnsAsync(hostOrder);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithValidationError((int ErrorCode, string ErrorInfo)? error)
    {
        _validationService.Setup(x => x.ValidateAsync(It.IsAny<HostOrder>())).ReturnsAsync(error);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithDropOrderEligibleForDoppelSpiel(Guid? correlationId)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextDropOrderEligibleForDoppelspiel(It.IsAny<string>())).ReturnsAsync(correlationId);
        return this;
    }

    public HostOrderConsumerTestFixture<TConsumer> WithNextPickOrder(HostOrder? hostOrder)
    {
        HostOrderRepositoryMock.Setup(x => x.GetNextPickForSources(It.IsAny<ICollection<string>>())).ReturnsAsync(hostOrder);
        return this;
    }

    public void VerifyHostOrderAdded(bool canRunDoppelspiel=true,Times times = default)
    {
        HostOrderRepositoryMock.Verify(x => x.Add(It.Is<HostOrder>(ho => ho.CanRunDoppelspiel == canRunDoppelspiel)), times);
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