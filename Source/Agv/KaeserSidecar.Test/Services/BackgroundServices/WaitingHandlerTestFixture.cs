using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

public class WaitingHandlerTestFixture : MassTransitTestFixture
{
    public WaitingHandler Handler { get; }

    private readonly StationRepository _stationRepository;
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IJamStateRepository> _jamStateRepository = new();
    private readonly SemaphoreProvider<IWaitingHandlerSemaphore> _semaphore = new(new Mock<ILogger<SemaphoreProvider<IWaitingHandlerSemaphore>>>().Object);
    private readonly Mock<IPrismatCommunicationShortLoopWeTargetStationRepository> _shortLoopWeTargetStationRepository = new();
    private List<WaitingPointAction> _actions = [];

    public Mock<IWaitingPointActionRepository> WaitingPointRepository { get; } = new();

    private List<ActionGroup> ActionGroups { get; } = new();

    public const string Target = "PS03";

    public static Guid TargetId = Target.ToGuid();

    public WaitingHandlerTestFixture()
    {
        AddServices(collection =>
        {
            collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
            collection.AddSingleton<IStationRepository, StationRepository>();
            collection.AddTransient(_ => _transportOrderDb.Object);
            collection.AddTransient(_ => _jamStateRepository.Object);
            collection.AddSingleton<IWaitingHandler, WaitingHandler>();
            collection.AddSingleton<ISemaphoreProvider<IWaitingHandlerSemaphore>>(_ => _semaphore);
            collection.AddTransient(_ => WaitingPointRepository.Object);
            collection.AddTransient(_ => _shortLoopWeTargetStationRepository.Object);
        });

        Build();
        WaitingPointRepository.Setup(x => x.RemoveById(It.IsAny<Guid>())).Verifiable();
        WaitingPointRepository.Setup(x => x.Add(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => { _actions.Add(action); });
        WaitingPointRepository.Setup(x => x.AddAsync(It.IsAny<WaitingPointAction>())).Callback((WaitingPointAction action) => _actions.Add(action));
        WaitingPointRepository.Setup(x => x.UpdateAsync(It.IsAny<WaitingPointAction>())).Verifiable();
        WaitingPointRepository.Setup(x => x.GetAll()).ReturnsAsync(() => _actions);
        WaitingPointRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => _actions.Single(a => a.CorrelationId == c));
        _stationRepository = (Provider.GetRequiredService<IStationRepository>() as StationRepository)!;
        Handler = (Provider.GetRequiredService<IWaitingHandler>() as WaitingHandler)!;
        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => { return ActionGroups.Single(a => a.Actions.Any(ac => ac.CorrelationId == c)); });
        _shortLoopWeTargetStationRepository.Setup(x => x.GetTargetStation()).ReturnsAsync(new ShortLoopWeTargetStation(Guid.NewGuid(), Target, Target));
    }

    public WaitingHandlerTestFixture WithWaitingPointAction(params WaitingPointAction[] actions)
    {
        _actions = actions.ToList();
        return this;
    }

    public WaitingHandlerTestFixture WithPalletJam(Guid? stationId)
    {
        _jamStateRepository.Setup(x => x.GetByStationAsync(It.IsAny<Guid>())).ReturnsAsync((Guid s) => new JamState(s, s == stationId ? 1 : 0, DateTime.UtcNow));
        return this;
    }

    public WaitingHandlerTestFixture WithOccupancy(StationOccupancy occupancy)
    {
        _stationRepository.WithOccupancies([occupancy]);
        return this;
    }

    public WaitingHandlerTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroups.Add(actionGroup);
        return this;
    }

    public async Task HandleAsync()
    {
        var cancellationToken = new CancellationTokenSource();
        _ = Handler.StartAsync(cancellationToken.Token);
        // We have to wait a bit to make sure that we are past the evaluation of cancellation token (happens only once at the beginning of the background task).
        // But we may not wait longer than the WaitingTime, as this might have lead to a second execution of the background loop already.
        // This is no ideal implementation, but a quick one which serves the current implementation to make sure the logic is executed once.
        await Task.Delay(Handler.WaitingTime * 0.9, cancellationToken.Token);
        await cancellationToken.CancelAsync();
        await Handler.StopAsync(CancellationToken.None);
    }

    public async Task VerifyStationOccupancyAsync(int count, Guid? agvCorrelationId = null)
    {
        ICollection<StationOccupancy> stationOccupancies = await _stationRepository.GetStationOccupancy();
        var stationOccupancy = stationOccupancies.SingleOrDefault(s => s.StationId == TargetId);
        stationOccupancy.ShouldNotBeNull();
        stationOccupancy.Count.ShouldBe(count);
        if (stationOccupancy.Count == 0)
        {
            stationOccupancy.AgvCorrelationId.ShouldBeNull();
        }
        else
        {
            if (agvCorrelationId != null)
            {
                stationOccupancy.AgvCorrelationId.ShouldBe(agvCorrelationId);
            }
        }
    }
}