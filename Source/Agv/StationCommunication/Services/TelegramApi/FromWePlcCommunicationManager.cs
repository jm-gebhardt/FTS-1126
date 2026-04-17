using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.TelegramApi;

/// <summary>
/// Manages the PLC connection for the FromWeStation (receiver) channel.
/// </summary>
[UsedImplicitly]
public class FromWePlcCommunicationManager(
    ILogger<FromWePlcCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)]
    Receiver<ConveyorTelegram> receiver,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)]
    Client client,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)]
    TelegramCommunicationConfiguration configuration,
    IPlcConnectionRepository connectionRepository,
    IBus bus)
    : PlcCommunicationManagerBase<Receiver<ConveyorTelegram>, ConveyorTelegram>(
        ServiceKeyProvider.FromWeStation, receiver, client, configuration, connectionRepository, bus, logger);
