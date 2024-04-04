using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv settings requests.
/// </summary>
[UsedImplicitly]
public class AgvSettingsRequestHandler :
    IConsumer<GetAgvSettings>,
    IConsumer<GetAgvsSettings>,
    IConsumer<UpdateDefaultAgvSettings>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;
    private readonly ILogger<AgvSettingsRequestHandler> _logger;

    /// <inheritdoc cref="AgvSettingsRequestHandler"/>
    public AgvSettingsRequestHandler(IAgvDb agvDb,
        ILogger<AgvSettingsRequestHandler> logger,
        IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
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
}