using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;

/// <summary>
/// Provides a dto for deleted restricted areas.
/// </summary>
/// <param name="Id">The id of the restricted area.</param>
[PublicAPI]
public record RestrictedAreaDeletedDto(Guid Id) : DatedMessage;