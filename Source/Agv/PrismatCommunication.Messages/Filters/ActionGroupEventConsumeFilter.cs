using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Services.Filters;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Filters;

/// <summary>
/// Provides consume filters for action group events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventConsumeFilter(
    ILogger<ActionGroupEventConsumeFilter> logger,
    UnTimedInMemoryFilter<ActionGroupCompleted> actionGroupCompletedFilter,
    UnTimedInMemoryFilter<ActionGroupStarted> actionGroupStartedFilter,
    UnTimedInMemoryFilter<PickActionCompleted> pickActionCompletedFilter) :
    IFilter<ConsumeContext<ActionGroupCompleted>>,
    IFilter<ConsumeContext<ActionGroupStarted>>,
    IFilter<ConsumeContext<PickActionCompleted>>
{
    /// <inheritdoc cref="IFilter{ActionGroupCompleted}"/>>
    public async Task Send(ConsumeContext<ActionGroupCompleted> context, IPipe<ConsumeContext<ActionGroupCompleted>> next)
    {
        if (await actionGroupCompletedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not consuming duplication of {Message}", nameof(ActionGroupCompleted));
    }

    /// <inheritdoc cref="IFilter{ActionGroupStarted}"/>>
    public async Task Send(ConsumeContext<ActionGroupStarted> context, IPipe<ConsumeContext<ActionGroupStarted>> next)
    {
        if (await actionGroupStartedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not consuming duplication of {Message}", nameof(ActionGroupStarted));
    }

    /// <inheritdoc cref="IFilter{ActionGroupStarted}"/>>
    public async Task Send(ConsumeContext<PickActionCompleted> context, IPipe<ConsumeContext<PickActionCompleted>> next)
    {
        if (await pickActionCompletedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not consuming duplication of {Message}", nameof(PickActionCompleted));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(ActionGroupEventConsumeFilter));
    }
}