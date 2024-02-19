using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv specification-related requests.
/// </summary>
[UsedImplicitly]
public class GetAgvBatteryRequestHandler :
    IConsumer<GetAgvBattery>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;

    /// <summary>
    /// Initializes a new <see cref="GetAgvDataRequestHandler"/> instance.
    /// </summary>
    public GetAgvBatteryRequestHandler(
        IAgvDb agvDb,
        ISettingsProvider<AgvSettings> settingsProvider,
        IMapper mapper
    )
    {
        _agvDb = agvDb;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetAgvBattery> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        var agvSettings = _mapper.Map<Messages.MessageContent.AgvSettings>(_settingsProvider.GetSettings());
        var agvStatus = _mapper.Map<AgvStatus>(agv.AgvStatus);
        var agvBatteryStatus = new AgvBatteryStatus(agvStatus, agvSettings);

        await context.RespondAsync(new GotAgvBattery(agvBatteryStatus));
    }
}