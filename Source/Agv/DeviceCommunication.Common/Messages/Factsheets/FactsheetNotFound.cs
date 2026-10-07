using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;

/// <summary>
/// Provides a response to the <see cref="GetFactsheet"/> request if no factsheet exists.
/// </summary>
/// <param name="CorrelationId">The id of the AGV type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type.</param>
[PublicAPI]
public record FactsheetNotFound(Guid CorrelationId, string AgvTypeIdentifier) : CorrelatedBy<Guid>;