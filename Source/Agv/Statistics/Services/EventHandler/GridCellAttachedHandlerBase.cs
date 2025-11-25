using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// An abstract base class that handles messages associated with grid cells.
/// Subclasses should implement the specific handling of a message for the existing grid IDs.
/// </summary>
/// <typeparam name="TMessage">The type of the message being handled.</typeparam>
public abstract class GridCellAttachedHandlerBase<TMessage> : IConsumer<TMessage>
    where TMessage : class, IAgvSerialNumber, IWithAgvPosition
{
    private readonly IStatisticsDb _statisticsDb;

    /// <summary>
    /// Logger
    /// </summary>
    protected ILogger<GridCellAttachedHandlerBase<TMessage>> Logger { get; }

    /// <summary>MassTransit publish endpoint./// </summary>
    protected IPublishEndpoint PublishEndpoint { get; }

    /// <inheritdoc cref="GridCellAttachedHandlerBase{TMessage}"/>
    protected GridCellAttachedHandlerBase(
        IStatisticsDb statisticsDb,
        IPublishEndpoint publishEndpoint,
        ILogger<GridCellAttachedHandlerBase<TMessage>> logger
    )
    {
        _statisticsDb = statisticsDb;
        Logger = logger;
        PublishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Handles a message with the already persisted grid IDs.
    /// </summary>
    /// <param name="message">The message object.</param>
    /// <param name="gridCells"></param>
    protected abstract Task HandleAsync(TMessage message, List<GridCell> gridCells);

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var message = context.Message;

        List<GridCell> gridCells = await _statisticsDb.GridCell.GetOrAddGridCellsForCoordinatesAsync((decimal) message.AgvPosition.X, (decimal) message.AgvPosition.Y);

        Logger.LogDebug("Handling {MessageType} for AGV {SerialNumber} with position {Position} and grid cell Ids {GridIds}",
            typeof(TMessage).Name, message.SerialNumber, message.AgvPosition, gridCells.Select(g => g.CellId));

        await HandleAsync(message, gridCells);
    }
}