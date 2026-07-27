using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class SimulatedAgvStateMachineTestFixture(TimeSpan? inactivityTimeout = null)
    : BaseStateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgv>(inactivityTimeout)
{
    public const string LoadId = "Load123";
    public const string LoadType = "pallet";
    public const string LoadPosition = "front";

    private Options _options = new()
    {
        InitialBatteryCharge = 75,
        InitialBatteryChargeDeviation = 0,
        BatteryChargeRate = 1.0,
        BatteryDischargeRate = 0.1,
        VisualizationUpdateInterval = 60_000,
        StateUpdateInterval = 60_000
    };

    // ReSharper disable once UnusedMethodReturnValue.Global
    protected SimulatedAgvStateMachineTestFixture WithOptions(Options options)
    {
        _options = options;
        return this;
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([typeof(Vda5050.Simulator.Messages.Profiles.OrderProfile).Assembly]);
    }

    public new SimulatedAgvStateMachineTestFixture Build()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(_options);
            sc.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
            sc.AddTransient<INurbsEdgeBuilder>(_ => new Mock<INurbsEdgeBuilder>().Object);
            sc.AddTransient<IAgvCache>(_ => new Mock<IAgvCache>().Object);
            sc.AddTransient<IAgvStateCache>(_ => new Mock<IAgvStateCache>().Object);
        });
        base.Build();
        return this;
    }

    public SimulatedAgv GetSaga(AgvConfiguration configuration)
    {
        return SagaHarness.Sagas.Select(s => s.CorrelationId == configuration.CorrelationId).First().Saga;
    }
}