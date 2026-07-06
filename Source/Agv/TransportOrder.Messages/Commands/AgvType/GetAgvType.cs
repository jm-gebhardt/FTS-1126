using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Provides a request to get an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
public record GetAgvType(Guid CorrelationId) : CorrelatedBy<Guid>;