using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Stops charging if Agv is in charging node
/// </summary>
[PublicAPI]
public class StopChargingCommandConsumer : IConsumer<StopChargingCommand>
{
    private readonly IAgvRegistry _agvRegistry;

    /// <summary>
    /// <inheritdoc cref="StopChargingCommandConsumer" />
    /// </summary>
    /// <param name="agvRegistry"></param>
    public StopChargingCommandConsumer(IAgvRegistry agvRegistry)
    {
        _agvRegistry = agvRegistry;
    }

    /// <summary>
    /// If the Agv is charging it stops the charging
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="AgvStatusReceivedNotFoundException"></exception>
    /// <exception cref="NotInCorrectStateException"></exception>
    public async Task Consume(ConsumeContext<StopChargingCommand> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agvInformation = _agvRegistry.GetAgvInformation(correlationId);

        if (agvInformation?.AgvStatusReceived == null)
        {
            throw new AgvStatusReceivedNotFoundException(correlationId);
        }
        
        if (!agvInformation.AgvStatusReceived.AgvStatus.Charging)
        {
            throw new NotInCorrectStateException("Agv cannot stop charging because agv is not in charging state.");
        }

        // the end implementation should be to send a cancel action command for the current (running) charging action, which will stop the charging internally.
        // But there is no implementation for that yet, you have to send a mqtt message to TAR in order to cancel the action
        await context.Publish(new AgvStoppedCharging(correlationId));
        await context.RespondAsync(new BaseResult());
    }
}