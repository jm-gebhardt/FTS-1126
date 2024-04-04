using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv specification-related requests.
/// </summary>
[UsedImplicitly]
public class GetAgvBatteryRequestHandler :
    IConsumer<GetAgvBattery>,
    IConsumer<GetAgvsBattery>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="GetAgvDataRequestHandler"/> instance.
    /// </summary>
    public GetAgvBatteryRequestHandler(
        IAgvDb agvDb,
        IMapper mapper
    )
    {
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetAgvBattery> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        var agvSettings = _mapper.Map<Messages.MessageContent.AgvSettings>(_agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup));
        var agvStatus = _mapper.Map<AgvStatus>(agv.AgvStatus);
        var agvBatteryStatus = new AgvBatteryStatus(agvStatus, agvSettings);

        await context.RespondAsync(new GotAgvBattery(agvBatteryStatus));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetAgvsBattery> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        ICollection<Domain.Models.Agv.Agv> agvs = await _agvDb.Agvs.SelectExistingAgvsAsync(selectedAgvs.ToList());

        List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();
        if (agvCorrelationIds.Count != selectedAgvs.Count && selectedAgvs.Any())
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvCorrelationIds).ToList());
        }

        List<AgvBatteryStatus> batteryStatus = agvs.Select(agv =>
        {
            var agvSettings = _mapper.Map<Messages.MessageContent.AgvSettings>(_agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup));
            var agvStatus = _mapper.Map<AgvStatus>(agv.AgvStatus);
            var agvBatteryStatus = new AgvBatteryStatus(agvStatus, agvSettings);
            return agvBatteryStatus;
        }).ToList();

        await context.RespondAsync(new GotAgvsBattery(batteryStatus));
    }
}