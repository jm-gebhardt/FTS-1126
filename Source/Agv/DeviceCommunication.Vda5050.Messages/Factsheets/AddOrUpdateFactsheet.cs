using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;

/// <summary>
/// Provides a command to add or update a factsheet.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Factsheet">The factsheet.</param>
/// <param name="RawJson">The raw factsheet JSON.</param>
public record AddOrUpdateFactsheet(Guid CorrelationId, Factsheet Factsheet, string RawJson = "") : CorrelatedBy<Guid>;