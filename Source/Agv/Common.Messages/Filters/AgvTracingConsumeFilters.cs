using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Filters;

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
        LogInformation(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<AgvErrorStatusReceived> context, IPipe<ConsumeContext<AgvErrorStatusReceived>> next)
    {
        LogInformation(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(AgvTracingConsumeFilters));
    }
}