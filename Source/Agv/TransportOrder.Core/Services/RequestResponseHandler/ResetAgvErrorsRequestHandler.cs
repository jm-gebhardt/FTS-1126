using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.MassTransit;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

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
    private readonly ILoggingService _loggingService;

    private readonly IBus _bus;

    /// <inheritdoc cref="ResetAgvErrorsRequestHandler"/>
    public ResetAgvErrorsRequestHandler(
        IAgvDb agvDb,
        IMessageAwaiter<AgvErrorChanged> agvErrorChangedAwaiter,
        IBus bus,
        ILogger<ResetAgvErrorsRequestHandler> logger,
        ILoggingService loggingService)
    {
        _agvDb = agvDb;
        _agvErrorChangedAwaiter = agvErrorChangedAwaiter;
        _logger = logger;
        _bus = bus;
        _loggingService = loggingService;
    }

    private bool AreAllErrorsResolved(ICollection<Guid> errorIdsToReset, AgvErrorChanged errorChange)
    {
        List<AgvError> unresolvedErrors =
            // all errors must be resolved
            errorIdsToReset.Count == 0 ?
                errorChange.Errors :
                // or only the specified errors
                errorChange.Errors.Where(error => errorIdsToReset.Contains(error.CorrelationId)).ToList();

        bool allErrorsResolved = unresolvedErrors.Count == 0;
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
        string user = context.Message.User;
        List<Guid> errorIdsToReset = context.Message.ErrorIds.ToList();
        var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, new CancellationTokenSource(_resetTimeout).Token).Token;
        IReadOnlyList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);

        // agv does not have any errors
        if (!activeErrors.Any())
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(correlationId);
            _logger.LogInformation("No active errors for Agv {AgvCorrelationId}", correlationId);
            await context.RespondAsync(new ResetAgvErrorsResult("No active errors. So all good already."));
            return;
        }

        List<AgvError> errorsToReset;
        if (errorIdsToReset.Count != 0)
        {
            // only reset specified active errors
            errorsToReset = activeErrors.Where(error => errorIdsToReset.Contains(error.CorrelationId)).ToList();
            if (errorsToReset.Any(e => e.IsQueueBlockingError()))
            {
                _logger.LogInformation("Queue blocking error reset via API for agv {AgvCorrelationId}", correlationId);
            }
            errorIdsToReset = errorsToReset.Select(e => e.CorrelationId).ToList();
            if (errorIdsToReset.Count == 0)
            {
                await context.RespondAsync(new ResetAgvErrorsResult("None of the specified errors are active. So all good already."));
                return;
            }
        }
        else
        {
            // reset all active errors
            errorsToReset = activeErrors.ToList();
        }

        try
        {
            List<IGrouping<AgvErrorSource, AgvError>> errorsBySource = errorsToReset.GroupBy(e => e.Source).ToList();
            await _agvErrorChangedAwaiter.WaitAsync(correlationId,
                errorChange => AreAllErrorsResolved(errorIdsToReset, errorChange),
                cancellationToken,
                executeAfterListeningTask: async () =>
                {
                    foreach (IGrouping<AgvErrorSource, AgvError> errorGroup in errorsBySource)
                    {
                        if (errorGroup.Key == AgvErrorSource.System)
                        {
                            await _bus.Publish(new ResolveSystemAgvError(correlationId, errorGroup.ToList()), cancellationToken);
                        }
                        else
                        {
                            await _bus.Publish(new Common.Messages.AgvManagement.Commands.ResetAgvErrors(correlationId, errorGroup.Select(e => e.CorrelationId).ToList()), cancellationToken);
                        }
                    }
                },
                checkContinuouslyUntilSuccess: errorsBySource.Count > 1);

            int numberOfResetErrors = errorIdsToReset.Count == 0 ? activeErrors.Count : errorIdsToReset.Count;
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvErrorReset} });
            await context.RespondAsync(new ResetAgvErrorsResult($"{numberOfResetErrors} error{(numberOfResetErrors == 1 ? "" : "s")} were reset successfully."));
        }
        catch (WaitForMessageFailedException<AgvErrorChanged> e)
        {
            throw new ResetAgvErrorFailedException(e.Message, e.ReceivedMessage?.Errors);
        }
    }
}