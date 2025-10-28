using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Commands;

/// <summary>
/// Provides a command to retract a host order and register its pick for Doppelspiel
/// </summary>
/// <param name="CorrelationId">The correlation id of the host order.</param>
/// <remarks>
/// <para><b>Possible Responses:</b></para>
/// <list type="bullet">
///   <item><see cref="Success"/> - Successfully retracted.</item>
///   <item><see cref="Failed"/> - Retraction failed.</item>
/// </list>
/// </remarks>
[UsedImplicitly]
public record RetractHostOrder(Guid CorrelationId) : CorrelatedBy<Guid>;