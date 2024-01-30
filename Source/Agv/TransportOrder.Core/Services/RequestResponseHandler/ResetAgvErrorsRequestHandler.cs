using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Responsible for resetting agv errors.
/// </summary>
[UsedImplicitly]
public class ResetAgvErrorsRequestHandler : IConsumer<ResetAgvErrors>
{
    private readonly TimeSpan _resetTimeout = TimeSpan.FromSeconds(10);
    private readonly ILogger<ResetAgvErrorsRequestHandler> _logger;
    private readonly IAgvDb _agvDb;
    private readonly IMessageAwaiter<AgvErrorChanged> _agvErrorChangedAwaiter;

    /// <inheritdoc cref="ResetAgvErrorsRequestHandler"/>
    /// <param name="agvDb"></param>
    /// <param name="agvErrorChangedAwaiter"></param>
    /// <param name="logger"></param>
    public ResetAgvErrorsRequestHandler(
        IAgvDb agvDb,
        IMessageAwaiter<AgvErrorChanged> agvErrorChangedAwaiter,
        ILogger<ResetAgvErrorsRequestHandler> logger)
    {
        _agvDb = agvDb;
        _agvErrorChangedAwaiter = agvErrorChangedAwaiter;
        _logger = logger;
    }

    private bool AreAllErrorsResolved(ICollection<Guid> errorIdsToReset, AgvErrorChanged errorChange)
    {
        List<AgvError> unresolvedErrors =
            // all errors must be resolved
            !errorIdsToReset.Any() ?
                errorChange.Errors :
                // or only the specified errors
                errorChange.Errors.Where(error => errorIdsToReset.Contains(error.CorrelationId)).ToList();

        bool allErrorsResolved = !unresolvedErrors.Any();
        if (!allErrorsResolved)
        {
            _logger.LogWarning("Following Agv errors were not resolved: {Errors}", unresolvedErrors);
        }
        return allErrorsResolved;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ResetAgvErrors> context)
    {
        var correlationId = context.Message.CorrelationId;
        List<Guid> errorIdsToReset = context.Message.ErrorIds.ToList();
        var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, new CancellationTokenSource(_resetTimeout).Token).Token;
        IList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);

        // agv does not have any errors
        if (!activeErrors.Any())
        {
            await context.RespondAsync(new ResetAgvErrorsResult("No active errors. So all good already."));
            return;
        }

        // errors to reset are specified
        if (errorIdsToReset.Any())
        {
            // but only try to reset active ones
            errorIdsToReset = activeErrors.Where(error => errorIdsToReset.Contains(error.CorrelationId)).Select(e => e.CorrelationId).ToList();
            if (!errorIdsToReset.Any())
            {
                await context.RespondAsync(new ResetAgvErrorsResult("None of the specified errors are active. So all good already."));
                return;
            }
        }

        try
        {
            await _agvErrorChangedAwaiter.WaitAsync(correlationId,
                errorChange => AreAllErrorsResolved(errorIdsToReset, errorChange),
                cancellationToken,
                executeAfterListeningTask: async () => { await context.Publish(new Common.Messages.AgvManagement.Commands.ResetAgvErrors(correlationId, errorIdsToReset), cancellationToken); });

            int numberOfResetErrors = errorIdsToReset.Count == 0 ? activeErrors.Count : errorIdsToReset.Count;
            await context.RespondAsync(new ResetAgvErrorsResult($"{numberOfResetErrors} error{(numberOfResetErrors == 1 ? "" : "s")} were reset successfully."));
        }
        catch (WaitForMessageFailedException<AgvErrorChanged> e)
        {
            throw new ResetAgvErrorFailedException(e.Message, e.ReceivedMessage?.Errors);
        }
    }
}