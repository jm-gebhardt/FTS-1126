using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// agv tracing.
/// </summary>
[UsedImplicitly]
public class AgvTracingConsumeFilters : BaseTracingFilter,
    IFilter<ConsumeContext<RequestAgvGraph>>,
    IFilter<ConsumeContext<AgvErrorStatusReceived>>
{
    /// <inheritdoc cref="AgvTracingConsumeFilters"/>>
    public AgvTracingConsumeFilters(ILogger<AgvTracingConsumeFilters> logger) : base(logger)
    {
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<RequestAgvGraph> context, IPipe<ConsumeContext<RequestAgvGraph>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<AgvErrorStatusReceived> context, IPipe<ConsumeContext<AgvErrorStatusReceived>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(AgvTracingConsumeFilters));
    }
}