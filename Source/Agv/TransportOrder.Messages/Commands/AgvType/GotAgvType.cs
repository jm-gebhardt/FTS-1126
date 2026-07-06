using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Represents the response to the <see cref="GetAgvType"/> request. 
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="AgvType">The agv type.</param>
public record GotAgvType(Guid CorrelationId, Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes.AgvType AgvType) : CorrelatedBy<Guid>;
