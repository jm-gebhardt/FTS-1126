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
/// Manages the PLC connection for the AgvCargoState (sender) channel.
/// </summary>
[UsedImplicitly]
public class AgvCargoStateCommunicationManager(
    ILogger<AgvCargoStateCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    Sender<ConveyorTelegram> sender,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    Client client,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    TelegramCommunicationConfiguration configuration,
    IPlcConnectionRepository connectionRepository,
    IBus bus)
    : PlcCommunicationManagerBase<Sender<ConveyorTelegram>, ConveyorTelegram>(
        ServiceKeyProvider.AgvCargoState, sender, client, configuration, connectionRepository, bus, logger);
