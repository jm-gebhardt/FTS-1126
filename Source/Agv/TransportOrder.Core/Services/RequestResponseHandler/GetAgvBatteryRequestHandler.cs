using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

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

        var agvSettings = _mapper.Map<AgvSettings>(_agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup));
        var agvStatus = _mapper.Map<AgvStatus>(agv.AgvStatus);
        var agvBatteryStatus = new AgvBatteryStatus(agvStatus);

        await context.RespondAsync(new GotAgvBattery(agvBatteryStatus, agvSettings));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetAgvsBattery> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        ICollection<Domain.Models.Agv.Agv> agvs = await _agvDb.Agvs.SelectExistingAgvsAsync(selectedAgvs.ToList());

        List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();
        if (agvCorrelationIds.Count != selectedAgvs.Count && selectedAgvs.Count != 0)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvCorrelationIds).ToList());
        }

        List<(AgvBatteryStatus agvBatteryStatus, AgvSettings agvSettings)> batteryStatus = agvs.Select(agv =>
        {
            var agvSettings = _mapper.Map<AgvSettings>(_agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup));
            var agvStatus = _mapper.Map<AgvStatus>(agv.AgvStatus);
            var agvBatteryStatus = new AgvBatteryStatus(agvStatus);
            return (agvBatteryStatus, agvSettings);
        }).ToList();

        await context.RespondAsync(new GotAgvsBattery(batteryStatus));
    }
}