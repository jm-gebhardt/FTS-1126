using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;
using MessageAgvAction = Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc />
public class StartActionService : IStartActionService
{
    private readonly IBus _bus;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="StartActionService"/> instance.
    /// </summary>
    public StartActionService(IBus bus, IMapper mapper)
    {
        _bus = bus;
        _mapper = mapper;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public async Task StartAction(IAgv agv, AgvAction action, CancellationToken cancellationToken)
    {
        if (action.Nodes == null || !action.Nodes.Any())
        {
            throw new ArgumentNullException(nameof(action.Nodes));
        }

        await _bus.Publish(new StartActionCommand(agv.CorrelationId, agv.AgvId, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
    }
}