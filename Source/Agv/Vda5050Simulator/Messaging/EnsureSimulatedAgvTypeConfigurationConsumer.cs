using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;
using MassTransit;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

/// <summary>
/// Handles <see cref="EnsureSimulatedAgvTypeConfiguration"/>: stores the simulation configuration of an agv type unless it already exists.
/// </summary>
public sealed class EnsureSimulatedAgvTypeConfigurationConsumer(Runtime.SimulatedAgvTypeConfigurationService service)
    : IConsumer<EnsureSimulatedAgvTypeConfiguration>
{
    public async Task Consume(ConsumeContext<EnsureSimulatedAgvTypeConfiguration> context)
    {
        (_, string agvTypeIdentifier, var settings) = context.Message;
        var dto = new AddAgvTypeConfigurationDto
        {
            AgvTypeIdentifier = agvTypeIdentifier,
            NoOfAgvs = settings.NoOfAgvs,
            StateUpdateIntervalMs = settings.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = settings.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = settings.BatteryDischargeRate,
            BatteryChargeRate = settings.BatteryChargeRate,
            InitialBatteryCharge = settings.InitialBatteryCharge,
            InitialBatteryChargeDeviation = settings.InitialBatteryChargeDeviation
        };
        await service.EnsureAsync(agvTypeIdentifier, dto);
        await context.RespondAsync(new Success());
    }
}
