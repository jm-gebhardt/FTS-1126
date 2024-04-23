using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using MassTransit;

namespace Gebhardt.Acc.WCSCommunication.Configuration;

/// <summary>
/// Provides a consume filter for action group events.
/// </summary>
public class ActionEventConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class

{
    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (typeof(T).GetInterfaces().Any(i => i.Name == nameof(IActionGroupEvent)))
        {
            if (((IActionGroupEvent)context.Message).ActionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
            {
                await next.Send(context);
            }
        }
        else
        {
            await next.Send(context);
        }
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
    }
}