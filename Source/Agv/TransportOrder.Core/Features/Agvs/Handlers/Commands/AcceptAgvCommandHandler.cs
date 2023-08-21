using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;

/// <summary>
/// Defines a handler for the <see cref="AcceptAgvCommand"/>
/// </summary>
[UsedImplicitly]
public class AcceptAgvCommandHandler : IConsumer<AcceptAgvCommand>
{
    private readonly IRequestClient<AcceptAgv> _acceptAgvClient;

    /// <summary>
    /// <inheritdoc cref="AcceptAgvCommandHandler"/>
    /// </summary>
    /// <param name="acceptAgvClient"></param>
    public AcceptAgvCommandHandler(IRequestClient<AcceptAgv> acceptAgvClient)
    {
        _acceptAgvClient = acceptAgvClient;
    }

    /// <summary>
    /// <inheritdoc cref="AcceptAgvCommandHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<AcceptAgvCommand> context)
    {
        Response<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult> response =
            await _acceptAgvClient.GetResponse<AcceptAgvSucceeded, AcceptAgvRejected, AgvNotFoundResult>(new AcceptAgv(context.Message.CorrelationId));

        if (response.Message is AgvNotFoundResult notFound)
        {
            await context.RespondAsync(notFound);
            return;
        }

        if (response.Message is AcceptAgvRejected rejected)
        {
            await context.RespondAsync(rejected);
            return;
        }

        await context.RespondAsync((AcceptAgvSucceeded)response.Message);
    }
}