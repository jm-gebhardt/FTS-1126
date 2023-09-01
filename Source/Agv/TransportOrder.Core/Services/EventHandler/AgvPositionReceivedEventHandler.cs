using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumes <see cref="AgvPositionReceived"/> and populates <see cref="IAgvPositionRegistry"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvPositionReceivedEventHandler : IConsumer<AgvPositionReceived>
{
    private readonly ILogger<AgvPositionReceivedEventHandler> _logger;
    private readonly IAgvPositionRegistry _agvPositionRegistry;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvPositionReceivedEventHandler(
        ILogger<AgvPositionReceivedEventHandler> logger,
        IAgvPositionRegistry agvPositionRegistry,
        IMapper mapper)
    {
        _logger = logger;
        _agvPositionRegistry = agvPositionRegistry;
        _mapper = mapper;
    }

    /// <summary>
    /// Update AGV position registry on receiving <see cref="AgvPositionReceived"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<AgvPositionReceived> context)
    {
        _logger.LogTrace(
            "{MessageType} {AgvId} / {CorrelationId} received",
            nameof(AgvPositionReceived),
            context.Message.AgvId,
            context.Message.CorrelationId
        );

        var agvPositionStatus = _mapper.Map<AgvPositionStatus>(context.Message);

        await _agvPositionRegistry.UpdatePositionAsync(agvPositionStatus);

    }
}