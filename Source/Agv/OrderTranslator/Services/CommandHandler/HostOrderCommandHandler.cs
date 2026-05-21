using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;

namespace Gebhardt.Acc.OrderTranslator.Services.CommandHandler;

/// <summary>
/// Handles host order related requests.
/// </summary>
[UsedImplicitly]
public class HostOrderCommandHandler(
    ILogger<HostOrderCommandHandler> logger,
    IHostOrderRepository hostOrderRepository,
    IStationRepository stationRepository,
    IHostOrderValidationService validationService) :
    IConsumer<Messages.CreateActionGroup>

{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<Messages.CreateActionGroup> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;

        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, hu, orderDetails.CreationDate)
        {
            AssignedAgv = context.Message.AssignedAgv
        };

        (Dictionary<InfoToAgvKey, string>? pickInfoToAgv, Dictionary<InfoToAgvKey, string>? dropInfoToAgv) = GeneratePickAndDropInformation(context.Message);
        logger.LogDebug("pickInfoToAgv: {PickInfoToAgv}", pickInfoToAgv == null ? "empty" : string.Join(", ", pickInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        logger.LogDebug("dropInfoToAgv: {DropInfoToAgv}", dropInfoToAgv == null ? "empty" : string.Join(", ", dropInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        hostOrder.AddAction(orderDetails.Source, ActionType.Pick, pickInfoToAgv);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop, dropInfoToAgv);

        ICollection<Station> pickStations = await stationRepository.GetStationsByLane(hostOrder.Source);
        ICollection<Station> dropStations = await stationRepository.GetStationsByLane(hostOrder.Target);
        if (pickStations.Count == 0 && dropStations.Count == 0)
        {
            logger.LogError("No station configuration for host order {OrderId}", hostOrder.CorrelationId);
            await context.Publish(new LogOccurred(new LogEntryDto
            {
                Id = Guid.Empty,
                Category = Categories.Order,
                LogCode = LogCodes.HostOrderNoStationConfiguration,
                LogLevel = nameof(LogLevel.Error),
                ActionGroupCorrelationId = hostOrder.CorrelationId,
                Timestamp = DateTime.UtcNow,
                Variables = new Dictionary<string, string> { { LogLabels.OrderId, hostOrder.CorrelationId.ToString() } }
            }, [Categories.Order]));
            return;
        }

        ValidationError? validationError = await validationService.ValidateAsync(hostOrder);
        if (validationError != null)
        {
            logger.LogError("Host order validation failed {ValidationError}", validationError);
            await context.Publish(new HostOrderValidationFailed(hostOrder.CorrelationId, validationError.Value));
            return;
        }

        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();

        var actionGroup = hostOrder.CreateActionGroup(pickStations, dropStations);
        if (actionGroup == null)
        {
            logger.LogError("Host order {OrderId} could not be converted into action group", hostOrder.CorrelationId);
            await context.Publish(new LogOccurred(new LogEntryDto
            {
                Id = Guid.Empty,
                Category = Categories.Order,
                LogCode = LogCodes.HostOrderConversionFailed,
                LogLevel = nameof(LogLevel.Error),
                ActionGroupCorrelationId = hostOrder.CorrelationId,
                Timestamp = DateTime.UtcNow,
                Variables = new Dictionary<string, string> { { LogLabels.OrderId, hostOrder.CorrelationId.ToString() } }
            }, [Categories.Order]));
            return;
        }

        await context.Publish(new Common.Messages.ActionGroups.Commands.CreateActionGroup(actionGroup));
    }

    /// <summary>
    /// Generates pick information for a host order.
    /// </summary>
    /// <param name="createHostOrderMessage">The message to generate cargo rotation information from.</param>
    /// <returns></returns>
    private static (Dictionary<InfoToAgvKey, string>?, Dictionary<InfoToAgvKey, string>?) GeneratePickAndDropInformation(Messages.CreateActionGroup createHostOrderMessage)
    {
        Dictionary<InfoToAgvKey, string>? pickInfoToAgv = null;
        Dictionary<InfoToAgvKey, string>? dropInfoToAgv = null;

        var orderDetails = createHostOrderMessage.OrderDetails;

        if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator)
        {
            pickInfoToAgv = new Dictionary<InfoToAgvKey, string>();
            if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator)
            {
                pickInfoToAgv.Add(InfoToAgvKey.CargoRotation, orderDetails.LeRotation.ToString(CultureInfo.InvariantCulture));
            }
        }
        return (pickInfoToAgv, dropInfoToAgv);
    }
}