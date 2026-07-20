using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to switch the operating mode of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="OperatingMode">The new operating mode.</param>
public record RequestOperatingModeSwitch(Guid CorrelationId, AgvOperatingMode OperatingMode) : CorrelatedBy<Guid>;