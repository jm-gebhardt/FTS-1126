using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles requests for resending agv related information, e.g. <see cref="RequestAgvData"/> 
/// </summary>
[UsedImplicitly]
public class ResendCommandHandler(ILogger<ResendCommandHandler> logger, IAgvDataRepository agvDataRepository):
     IConsumer<RequestAgvData>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvData> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string agvSerialNumber = context.Message.SerialNumber;
        logger.LogInformation("(Re)sending of agv data requested for agv {AgvSerialNumber}/{AgvCorrelationId}", agvSerialNumber, agvCorrelationId);
        var agvData = await agvDataRepository.GetAgvDataAsync(agvCorrelationId);
        if (agvData != null)
        {
            await context.Publish(new AgvDataReceived(agvCorrelationId, agvSerialNumber, agvData));
            return;
        }
        logger.LogInformation("No data available for (re)sending of agv data requested for agv {AgvSerialNumber}/{AgvCorrelationId}", agvSerialNumber, agvCorrelationId);
    }
}