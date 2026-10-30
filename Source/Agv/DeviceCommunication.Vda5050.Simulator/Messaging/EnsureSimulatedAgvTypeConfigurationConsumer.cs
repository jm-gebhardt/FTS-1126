using DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using MassTransit;

namespace DeviceCommunication.Vda5050.Simulator.Messaging;

/// <summary>
/// Handles <see cref="EnsureSimulatedAgvTypeConfiguration"/>: stores the simulation configuration of an agv type unless it already exists.
/// </summary>
public sealed class EnsureSimulatedAgvTypeConfigurationConsumer(Runtime.AgvTypeConfigurationService service)
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
            InitialBatteryChargeDeviation = settings.InitialBatteryChargeDeviation,
            MaxSpeed = settings.MaxSpeed
        };
        await service.EnsureAsync(agvTypeIdentifier, dto);
        await context.RespondAsync(new Success());
    }
}
