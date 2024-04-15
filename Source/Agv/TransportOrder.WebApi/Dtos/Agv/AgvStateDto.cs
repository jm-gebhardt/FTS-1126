using Gebhardt.StoreWare.Agv.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv State Dto
/// </summary>
[PublicAPI]
public record AgvStateDto : DatedMessage
{
    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Current State</summary>
    public AgvStateType CurrentState { get; init; }
}