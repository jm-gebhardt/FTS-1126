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
/// Manages the PLC connection for the AplWeStation (sender) channel.
/// </summary>
[UsedImplicitly]
public class ToWePlcCommunicationManager(
    ILogger<ToWePlcCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    Sender<ConveyorTelegram> sender,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    Client client,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    TelegramCommunicationConfiguration configuration,
    IPlcConnectionRepository connectionRepository,
    IBus bus)
    : PlcCommunicationManagerBase<Sender<ConveyorTelegram>, ConveyorTelegram>(
        ServiceKeyProvider.AplWeStation, sender, client, configuration, connectionRepository, bus, logger);
