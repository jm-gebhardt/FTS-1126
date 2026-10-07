using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;

/// <summary>
/// Provides a response to the <see cref="GetFactsheet"/> request if the factsheet exists.
/// </summary>
/// <param name="CorrelationId">The id of the AGV type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type.</param>
/// <param name="FactsheetJson">The factsheet as raw JSON.</param>
[PublicAPI]
public record GotFactsheet(Guid CorrelationId, string AgvTypeIdentifier, string FactsheetJson) : CorrelatedBy<Guid>;