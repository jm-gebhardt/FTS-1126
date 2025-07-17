using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Provides a publishing filter for <see cref="AgvSetToPushOrJoystickMode"/>.
/// </summary>
[UsedImplicitly]
public class PushModePublishFilter(ILogger<PushModePublishFilter> logger, UnTimedInMemoryFilter<AgvSetToPushOrJoystickMode> pushModeSetFilter) : IFilter<PublishContext<AgvSetToPushOrJoystickMode>>
{
    /// <inheritdoc cref="IFilter{AgvSetToPushOrJoystickMode}"/>>
    public async Task Send(PublishContext<AgvSetToPushOrJoystickMode> context, IPipe<PublishContext<AgvSetToPushOrJoystickMode>> next)
    {
        if (await pushModeSetFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogDebug("Not sending duplication of {Message}", nameof(AgvSetToPushOrJoystickMode));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(PushModePublishFilter));
    }
}
