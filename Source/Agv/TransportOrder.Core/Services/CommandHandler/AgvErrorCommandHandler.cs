using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for agv error related commands.
/// </summary>
[UsedImplicitly]
public class AgvErrorCommandHandler : IConsumer<UpdateErrorStatus>
{
    private readonly IAgvErrorRepository _agvErrorRepository;

    /// <summary>
    /// Initializes a new <see cref="AgvErrorCommandHandler"/> instance.
    /// </summary>
    /// <param name="agvErrorRepository"></param>
    public AgvErrorCommandHandler(IAgvErrorRepository agvErrorRepository)
    {
        _agvErrorRepository = agvErrorRepository;
    }

    /// <summary>
    /// Handles the <see cref="UpdateErrorStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError>? errors) = context.Message;
        await _agvErrorRepository.AddOrUpdateAgvErrorStatusAsync(errors, correlationId);
    }
}