using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for agv error related commands.
/// </summary>
[UsedImplicitly]
public class AgvErrorCommandHandler : IConsumer<UpdateErrorStatus>
{
    private readonly IAgvErrorRepository _agvErrorRepository;
    private readonly ILogger<AgvErrorCommandHandler> _logger;

    /// <summary>
    /// Initializes a new <see cref="AgvErrorCommandHandler"/> instance.
    /// </summary>
    /// <param name="agvErrorRepository"></param>
    /// <param name="logger"></param>
    public AgvErrorCommandHandler(IAgvErrorRepository agvErrorRepository, ILogger<AgvErrorCommandHandler> logger)
    {
        _agvErrorRepository = agvErrorRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="UpdateErrorStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError>? errors) = context.Message;
        bool errorChanged = await _agvErrorRepository.AddOrUpdateAgvErrorStatusAsync(errors, correlationId);
        _logger.LogDebug("Received {Message} with {Errors} errors. Error changed: {ErrorChanged}", nameof(UpdateErrorStatus), errors, errorChanged);
        if (errorChanged)
        {
            await context.Publish(new AgvErrorChanged(context.Message.CorrelationId, errors));
        }
    }
}