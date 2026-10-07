using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;

/// <summary>
/// An event reporting that a factsheet was uploaded for an AGV type.
/// </summary>
/// <param name="CorrelationId">The id of the AGV type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type.</param>
/// <param name="FactsheetJson">The uploaded factsheet as raw JSON.</param>
[PublicAPI]
public record FactsheetUploaded(Guid CorrelationId, string AgvTypeIdentifier, string FactsheetJson) : CorrelatedBy<Guid>;