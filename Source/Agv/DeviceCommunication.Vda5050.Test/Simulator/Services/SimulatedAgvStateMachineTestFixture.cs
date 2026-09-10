using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class SimulatedAgvStateMachineTestFixture(TimeSpan? inactivityTimeout = null)
    : BaseStateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgv>(inactivityTimeout)
{
    public const string LoadId = "Load123";
    public const string LoadType = "pallet";
    public const string LoadPosition = "front";
    public const string MapId = "map";
    public const string Manufacturer = "gebhardt";
    public const string AgvTypeIdentifier = $"{Manufacturer}.simagv";
    public const int StateUpdateIntervalMs = 60_000;
    public const int VisualizationUpdateIntervalMs = 60_000;
    public const int ChangedStateUpdateIntervalMs = 30_000;
    public const int ChangedVisualizationUpdateIntervalMs = 45_000;
    public const double ChangedBatteryChargeRate = 5.0;
    public const double ChangedBatteryDischargeRate = 0.5;

    private const int SagaRemovalTimeoutSeconds = 1;

    private readonly Mock<IAgvOperatingModeCache> _operatingModeCache = new();
    private readonly Mock<IAgvStateCache> _stateCache = new();

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([typeof(Vda5050.Simulator.Messages.Profiles.OrderProfile).Assembly]);
    }

    public new SimulatedAgvStateMachineTestFixture Build()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
            sc.AddTransient<INurbsEdgeBuilder>(_ => new Mock<INurbsEdgeBuilder>().Object);
            sc.AddTransient<IAgvOperatingModeCache>(_ => _operatingModeCache.Object);
            sc.AddTransient<IAgvStateCache>(_ => _stateCache.Object);
        });
        base.Build();
        return this;
    }

    public SimulatedAgv GetSaga(AgvConfiguration configuration)
    {
        return SagaHarness.Sagas.Select(s => s.CorrelationId == configuration.CorrelationId).First().Saga;
    }

    public async Task ShouldHaveRemovedSagaAsync(Guid agvCorrelationId)
    {
        Guid? sagaId = await SagaHarness.NotExists(agvCorrelationId, TimeSpan.FromSeconds(SagaRemovalTimeoutSeconds));
        sagaId.ShouldBeNull();
    }

    public void ShouldHaveClearedCaches(Guid agvCorrelationId)
    {
        _operatingModeCache.Verify(cache => cache.RemoveAsync(agvCorrelationId), Times.Once);
        _stateCache.Verify(cache => cache.RemoveAsync(agvCorrelationId), Times.Once);
    }
}