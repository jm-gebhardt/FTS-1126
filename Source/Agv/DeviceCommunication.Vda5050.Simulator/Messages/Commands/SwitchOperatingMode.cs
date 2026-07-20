using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to switch the operating mode of the agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="OperatingMode">The new operating mode.</param>
public record SwitchOperatingMode(Guid CorrelationId, AgvOperatingMode OperatingMode): CorrelatedBy<Guid>;