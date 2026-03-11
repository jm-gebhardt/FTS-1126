using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices;

/// <summary>
/// Provides a background service for initializing prismat communication.
/// </summary>
public class InitializationProvider(IBus bus, ILogger<InitializationProvider> logger): BackgroundService
{
    private readonly RequestTimeout _requestTimeOut = RequestTimeout.After(m: 1);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool responseReceived= false;
        while (!responseReceived)
        {
            IRequestClient<RequestAgvAvailabilityStatus>? client = bus.CreateRequestClient<RequestAgvAvailabilityStatus>();
            try
            {
                Response<AgvAvailabilityStatusReceived> response = await client.GetResponse<AgvAvailabilityStatusReceived>(new RequestAgvAvailabilityStatus(), stoppingToken, _requestTimeOut);
                responseReceived = true;
                if (response.Message.HasAvailableAgvs)
                {
                    await bus.Publish(new SetReady(), stoppingToken);
                    return;
                }
                await bus.Publish(new SetNotReady(), stoppingToken);
            }
            catch (Exception)
            {
               logger.LogInformation("No response received on agv availability request, trying again");
               await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}