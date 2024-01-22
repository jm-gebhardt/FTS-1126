using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv settings requests.
/// </summary>
[UsedImplicitly]
public class GetAgvSettingsRequestHandler :
    IConsumer<GetAgvSettings>,
    IConsumer<GetAgvsSettings>
{
    private readonly ISettingsProvider<Domain.Models.Settings.AgvSettings> _agvSettingsProvider;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GetAgvSettingsRequestHandler"/>
    public GetAgvSettingsRequestHandler(IAgvDb agvDb,
        ISettingsProvider<Domain.Models.Settings.AgvSettings> agvSettingsProvider,
        IMapper mapper)
    {
        _agvSettingsProvider = agvSettingsProvider;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvSettings> context)
    {
        var correlationId = context.Message.CorrelationId;

        var agvSettings = _agvSettingsProvider.GetSettings();

        var agvSettingsMessage = _mapper.Map<AgvSettings>(agvSettings);
        _mapper.Map(correlationId, agvSettingsMessage);

        await context.RespondAsync(new GotAgvSettings(agvSettingsMessage));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvsSettings> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        var agvSettings = _agvSettingsProvider.GetSettings();
        await context.RespondAsync(new GotAgvsSettings(selectedAgvs.Select(correlationId =>
        {
            var agvSettingsMessage = _mapper.Map<AgvSettings>(agvSettings);
            _mapper.Map(correlationId, agvSettingsMessage);
            // we do not have specific AgvSettings yet, so just return the same for all Agvs.
            return agvSettingsMessage;
        }).ToList()));
    }
}