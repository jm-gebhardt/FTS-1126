using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Notification.Models;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv settings requests.
/// </summary>
[UsedImplicitly]
public class AgvSettingsRequestHandler :
    IConsumer<GetAgvSettings>,
    IConsumer<GetAgvsSettings>,
    IConsumer<UpdateDefaultAgvSettings>,
    IConsumer<UpdateAgvSettings>,
    IConsumer<UpdateAgvsSettings>,
    IConsumer<ResetAgvSettingToDefault>,
    IConsumer<ResetAgvsSettingToDefault>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly ILogger<AgvSettingsRequestHandler> _logger;
    private readonly ILoggingService _loggingService;
    private readonly ITransportOrderNotificationSender _notificationSender;

    /// <inheritdoc cref="AgvSettingsRequestHandler"/>
    public AgvSettingsRequestHandler(IAgvDb agvDb,
        ILogger<AgvSettingsRequestHandler> logger,
        ILoggingService loggingService,
        IBus bus,
        IMapper mapper,
        ITransportOrderNotificationSender notificationSender)
    {
        _agvDb = agvDb;
        _mapper = mapper;
        _bus = bus;
        _logger = logger;
        _loggingService = loggingService;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvSettings> context)
    {
        var correlationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        var agvSettings =  _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);

        var agvSettingsMessage = _mapper.Map<AgvSettings>(agvSettings);
        _mapper.Map(correlationId, agvSettingsMessage);

        await context.RespondAsync(new GotAgvSettings(agvSettingsMessage));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvsSettings> context)
    {
        var selectedAgvs = context.Message.CorrelationIds;
        List<AgvSettings> settings = new();
        foreach (var correlationId in selectedAgvs)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
            var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
            var agvSettingsMessage = _mapper.Map<AgvSettings>(agvSettings);
            settings.Add(_mapper.Map(correlationId, agvSettingsMessage));
        }
        await context.RespondAsync(new GotAgvsSettings(settings));
    }

    /// <summary>
    /// Handles the <see cref="UpdateDefaultAgvSettings"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateDefaultAgvSettings> context)
    {
        var settings = context.Message.UpdatedSettings;
        string user = context.Message.User;

        var defaultSettingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv();
        await _agvDb.AgvSettings.AddOrUpdateSettingsForAgvGroup(settings, defaultSettingsGroup.GroupType, defaultSettingsGroup.Identifier);
        await _loggingService.LogSystem(_logger, LogCodes.AgvOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.SystemAgvDefaultSettingUpdated } });

        await _notificationSender.SendAgvNotification(
            "Default agv settings changed",
            "User {user} updated the default agv settings", 
            new Dictionary<string, string> { { "user", user } },
            []
        );
        _logger.LogInformation("Default agv settings changed." );
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvSettings"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvSettings> context)
    {
        (var correlationId, var settings, string user) = context.Message;
        await UpdateAgvSetting(correlationId, settings);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvSettingUpdated } });

        await _notificationSender.SendAgvNotification(
            "Agv settings changed",
            "User {user} updated the settings for agv {agvId}",
            new Dictionary<string, string> {{"user", user}, {"agvId", correlationId.ToString()}},
            [ new NotificationLink("Agv", NotificationLinkResource.Agv, correlationId)]
        );
        
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvsSettings"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvsSettings> context)
    {
        (var correlationIds, var settings, string user) = context.Message;
        var updateTasks = correlationIds
            .Select(correlationId => Task.Run(() => UpdateAgvSetting(correlationId, settings))).ToList();

        await Task.WhenAll(updateTasks);
        foreach (var correlationId in correlationIds)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvSettingUpdated } });
            await _notificationSender.SendAgvNotification(
                "Agv settings changed",
                "User {user} updated the settings for agv {agvId}",
                new Dictionary<string, string>{{ "user", user }, { "agvId", correlationId.ToString() }},
                [ new NotificationLink("Agv", NotificationLinkResource.Agv, correlationId)]
            );
        }
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="ResetAgvSettingToDefault"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetAgvSettingToDefault> context)
    {
        (var correlationId, string propertyName, string user) = context.Message;
        await ResetAgvSettingToDefault(correlationId, propertyName);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvSettingsReset } });

        await _notificationSender.SendAgvNotification("Agv settings reset to default",
            "User {user} reset agv {agvId} to the default settings",
            new Dictionary<string, string>{{ "user", user }, { "agvId", correlationId.ToString() }},
            [ new NotificationLink("Agv", NotificationLinkResource.Agv, correlationId)]
        );
        
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="ResetAgvsSettingToDefault"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetAgvsSettingToDefault> context)
    {
        (var correlationIds, string propertyName, string user) = context.Message;
        var updateTasks = correlationIds
            .Select(correlationId => Task.Run(() => ResetAgvSettingToDefault(correlationId, propertyName))).ToList();

        await Task.WhenAll(updateTasks);
        foreach (var correlationId in correlationIds)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, correlationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvSettingsReset } });
            await _notificationSender.SendAgvNotification("Agv settings reset to default", "User {user} reset agv {agvId} to the default settings", new Dictionary<string, string> { { "user", user }, { "agvId", correlationId.ToString() } }, [new NotificationLink("Agv", NotificationLinkResource.Agv, correlationId)]);
        }
        await context.RespondAsync(new Success());
    }

    private async Task ResetAgvSettingToDefault(Guid correlationId, string propertyName)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _agvDb.AgvSettings.ResetSettingPropertyToDefaultAsync(agv.AgvSettingsGroup, propertyName);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvSettingsReset, agv.CorrelationId, null, "AGV Setting Reset", $"{propertyName} reset to default",
            new Dictionary<string, object?>{{LogLabels.PropertyName, propertyName}});
    }

    private async Task UpdateAgvSetting(Guid correlationId, AgvSettingsReadModel settings)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _agvDb.AgvSettings.AddOrUpdateSettingsForAgvGroup(settings, GroupType.SerialNumber, agv.SerialNumber);
        await _bus.Publish(new UpdateAgvSettingsGroupReference(correlationId));
        await LogSettingsUpdateForAgv(correlationId, settings);
    }

    private async Task LogSettingsUpdateForAgv(Guid correlationId, AgvSettingsReadModel settings)
    {
        foreach ((string name, object? value) in settings.GetValidPropertiesInformation())
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvSettingUpdated, correlationId, null, "AGV Setting Updated", $"{name} updated to {value}",
            new Dictionary<string, object?> { { LogLabels.PropertyName, name }, {LogLabels.PropertyValue, value} });
        }
    }
}