using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
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

    /// <inheritdoc cref="AgvSettingsRequestHandler"/>
    public AgvSettingsRequestHandler(IAgvDb agvDb,
        ILogger<AgvSettingsRequestHandler> logger,
        IBus bus,
        IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
        _bus = bus;
        _logger = logger;
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
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
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

        var defaultSettingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv();
        await _agvDb.AgvSettings.AddOrUpdateSettingsForAgvGroup(settings, defaultSettingsGroup.GroupType, defaultSettingsGroup.Identifier);
        _logger.LogInformation("Default agv settings changed." );
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvSettings"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvSettings> context)
    {
        var (correlationId, settings) = context.Message;
        await UpdateAgvSetting(correlationId, settings);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvsSettings"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvsSettings> context)
    {
        (ICollection<Guid> correlationIds, var settings) = context.Message;
        List<Task> updateTasks = correlationIds
            .Select(correlationId => Task.Run(() => UpdateAgvSetting(correlationId, settings))).ToList();

        await Task.WhenAll(updateTasks);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="ResetAgvSettingToDefault"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetAgvSettingToDefault> context)
    {
        (var correlationId, string propertyName) = context.Message;
        await ResetAgvSettingToDefault(correlationId, propertyName);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="ResetAgvsSettingToDefault"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetAgvsSettingToDefault> context)
    {
        (ICollection<Guid> correlationIds, string propertyName) = context.Message;
        List<Task> updateTasks = correlationIds
            .Select(correlationId => Task.Run(() => ResetAgvSettingToDefault(correlationId, propertyName))).ToList();

        await Task.WhenAll(updateTasks);
        await context.RespondAsync(new Success());
    }

    private async Task ResetAgvSettingToDefault(Guid correlationId, string propertyName)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _agvDb.AgvSettings.ResetSettingPropertyToDefaultAsync(agv.AgvSettingsGroup, propertyName);
        _logger.LogAgvInformation(LogCodes.AgvSettingsReset, agv.CorrelationId, "AGV Setting Reset", $"{propertyName} reset to default",
            new Dictionary<string, object?>{{LogLabels.PropertyName, propertyName}});
    }

    private async Task UpdateAgvSetting(Guid correlationId, AgvSettingsReadModel settings)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _agvDb.AgvSettings.AddOrUpdateSettingsForAgvGroup(settings, GroupType.SerialNumber, agv.SerialNumber);
        await _bus.Publish(new UpdateAgvSettingsGroupReference(correlationId));
        LogSettingsUpdateForAgv(correlationId, settings);
    }

    private void LogSettingsUpdateForAgv(Guid correlationId, AgvSettingsReadModel settings)
    {
        foreach ((string? name, object? value) in settings.GetValidPropertiesInformation())
        {
            _logger.LogAgvInformation(LogCodes.AgvSettingUpdated, correlationId, "AGV Setting Updated", $"{name} updated to {value}",
            new Dictionary<string, object?> { { LogLabels.PropertyName, name }, {LogLabels.PropertyValue, value} });
        }
    }
}