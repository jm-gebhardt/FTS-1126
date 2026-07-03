using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.FactSheet;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.FactSheet;

/// <summary>
/// Provides a command to add or update a fact sheet.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="FactSheet">The fact sheet.</param>
/// <param name="RawJson">The raw fact sheet JSON.</param>
public record AddOrUpdateFactSheet(Guid CorrelationId, Factsheet FactSheet, string RawJson = "") : CorrelatedBy<Guid>;