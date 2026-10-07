using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;

/// <summary>
/// Provides a request to get the factsheet of an AGV type.
/// </summary>
/// <param name="CorrelationId">The id of the AGV type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type.</param>
[PublicAPI]
public record GetFactsheet(Guid CorrelationId, string AgvTypeIdentifier) : CorrelatedBy<Guid>;