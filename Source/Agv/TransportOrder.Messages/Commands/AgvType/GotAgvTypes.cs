using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Represents the response to the <see cref="GetAllAgvTypes"/> request.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
/// <param name="AgvTypes">The agv types.</param>
public record GotAgvTypes(Guid CorrelationId, ICollection<Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes.AgvType> AgvTypes): CorrelatedBy<Guid>;

