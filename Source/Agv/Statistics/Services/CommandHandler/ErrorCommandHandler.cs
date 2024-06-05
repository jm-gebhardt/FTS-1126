using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Services.CommandHandler;

/// <summary>
/// Provides a handler for error-related commands.
/// </summary>
[UsedImplicitly]
public class ErrorCommandHandler(IStatisticsDb db) : IConsumer<AddError>
{
    /// <summary>
    /// Handles the <see cref="AddError"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddError> context)
    {
        var (agvError, positionId) = context.Message;

        var position = await db.GridAgvPositions.GetByIdAsync(positionId);

        if (agvError.IsFieldViolation())
        {
            var error = new AgvFieldViolation(position.PositionId, agvError.ErrorReceived);
            db.GridAgvFieldViolations.Add(error);
        }
        else
        {
            var error = new AgvError(position.PositionId,
                agvError.ErrorReceived,
                agvError.ErrorLevel,
                agvError.ErrorCode ?? "",
                agvError.ErrorType,
                agvError.ErrorCategory);
            db.GridAgvErrors.Add(error);
        }
      
        await db.SaveAsync();
    }
}