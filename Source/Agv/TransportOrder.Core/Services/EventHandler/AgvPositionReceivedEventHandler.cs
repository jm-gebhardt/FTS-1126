using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumes <see cref="AgvPositionReceived"/> and populates <see cref="IAgvPositionRepository"/>.
/// </summary>
[PublicAPI]
public class AgvPositionReceivedEventHandler : IConsumer<AgvPositionReceived>
{
    private readonly ILogger<AgvPositionReceivedEventHandler> _logger;
    private readonly IAgvPositionRepository _agvPositionRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvPositionReceivedEventHandler(
        ILogger<AgvPositionReceivedEventHandler> logger,
        IAgvPositionRepository agvPositionRepository,
        IMapper mapper)
    {
        _logger = logger;
        _agvPositionRepository = agvPositionRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Update AGV position repository on receiving <see cref="AgvPositionReceived"/>.
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

        await _agvPositionRepository.UpdatePositionAsync(agvPositionStatus);
    }
}