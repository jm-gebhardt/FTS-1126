using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group statistics related requests.
/// </summary>
[UsedImplicitly]
public class ActionGroupStatisticsRequestHandler(IActionGroupRepository actionGroupRepository) : IConsumer<GetActionGroupStatistics>
{
    /// <summary>
    /// Handles the <see cref="GetActionGroupStatistics"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroupStatistics> context)
    {
        (string? source, string? target) = context.Message;

        Dictionary<ActionType, ICollection<double>> actionDurations = await actionGroupRepository.GetActionDurationsByTypeAsync(ActionGroupState.Completed, source, target);
        List<DurationStatistics> statistics = [];
        foreach ((var actionType, ICollection<double> durations) in actionDurations)
        {
            statistics.Add(new DurationStatistics(actionType, durations.Median(), durations.Min(), durations.Max()));
        }

        await context.RespondAsync(new GotActionGroupStatistics(statistics));
    }
}