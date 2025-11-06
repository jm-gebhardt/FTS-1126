using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <summary>
/// Handles simulation commands to pause or resume order generation.
/// </summary>
[UsedImplicitly]
public class SimulationCommandHandler :
    IConsumer<PauseOrderGeneration>,
    IConsumer<ResumeOrderGeneration>,
    IConsumer<RequestHostOrderGeneration>
{
    private readonly IEnumerable<IHostOrderCreator> _hostOrderCreators;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SimulationCommandHandler> _logger;

    /// <inheritdoc cref="SimulationCommandHandler"/>
    public SimulationCommandHandler(IEnumerable<IHostOrderCreator> hostOrderCreators,
        IPublishEndpoint publishEndpoint,
        ILogger<SimulationCommandHandler> logger)
    {
        _hostOrderCreators = hostOrderCreators;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<PauseOrderGeneration> context)
    {
        foreach (var pollable in _hostOrderCreators)
        {
            pollable.Pause();
        }
        _logger.LogInformation("Order generation paused");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<ResumeOrderGeneration> context)
    {
        foreach (var pollable in _hostOrderCreators)
        {
            pollable.Resume();
        }
        _logger.LogInformation("Order generation resumed");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<RequestHostOrderGeneration> context)
    {
        string source = context.Message.Source;
        string destination = context.Message.Destination;

        var orderIds = new List<string>();
        for (int i = 0; i < context.Message.Count; i++)
        {
            string orderId = $"{source}->{destination}|{Guid.NewGuid().ToString().Split('-')[0]}";
            var createdDate = DateTime.UtcNow;
            var correlationId = Guid.NewGuid();
            string leNo = HostOrder.GetTransportOrderId(orderId);
            var orderDetails = new OrderDetails(correlationId, orderId, source, destination, leNo, HostOrderCreator.Simulation, createdDate, 0);

            await _publishEndpoint.Publish(new CreateHostOrder(orderDetails.OrderId, orderDetails), context.CancellationToken);
            _logger.LogInformation("Creating new simulation host order, {OrderId}", orderId);
            orderIds.Add(orderId);
        }

        await context.RespondAsync(new HostOrderGenerationResponse(orderIds));
    }
}