using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Services.Filters;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Provides publish filters for action events.
/// </summary>
[UsedImplicitly]
public class ActionEventPublishFilter(
    ILogger<ActionEventPublishFilter> logger,
    UnTimedInMemoryFilter<ActionCompletedByAgv> actionCompletedFilter,
    UnTimedInMemoryFilter<ActionStartedByAgv> actionStartedFilter,
    UnTimedInMemoryFilter<ActionUpdatedByAgv> actionUpdatedFilter,
    UnTimedInMemoryFilter<ActionAbortedByAgv> actionCanceledFilter) :
    IFilter<PublishContext<ActionCompletedByAgv>>,
    IFilter<PublishContext<ActionStartedByAgv>>,
    IFilter<PublishContext<ActionUpdatedByAgv>>,
    IFilter<PublishContext<ActionAbortedByAgv>>
{
    /// <inheritdoc cref="IFilter{ActionCompletedByAgv}"/>>
    public async Task Send(PublishContext<ActionCompletedByAgv> context, IPipe<PublishContext<ActionCompletedByAgv>> next)
    {
        if (await actionCompletedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionCompletedByAgv));
    }

    /// <inheritdoc cref="IFilter{ActionStartedByAgv}"/>>
    public async Task Send(PublishContext<ActionStartedByAgv> context, IPipe<PublishContext<ActionStartedByAgv>> next)
    {
        if (await actionStartedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionStartedByAgv));
    }

    /// <inheritdoc cref="IFilter{ActionUpdatedByAgv}"/>>
    public async Task Send(PublishContext<ActionUpdatedByAgv> context, IPipe<PublishContext<ActionUpdatedByAgv>> next)
    {
        if (await actionUpdatedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionUpdatedByAgv));
    }

    /// <inheritdoc cref="IFilter{ActionAbortedByAgv}"/>>
    public async Task Send(PublishContext<ActionAbortedByAgv> context, IPipe<PublishContext<ActionAbortedByAgv>> next)
    {
        if (await actionCanceledFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionAbortedByAgv));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(ActionEventPublishFilter));
    }
}