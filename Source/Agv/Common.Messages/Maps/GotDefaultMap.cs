using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Maps;

/// <summary>
///  Provides a response to the <see cref="GetDefaultMap"/> request.
/// </summary>
/// <param name="Map">The default map.</param>
/// <param name="CorrelationId">Correlation id of the map.</param>
[PublicAPI]
public record GotDefaultMap(Guid CorrelationId, Map Map);

