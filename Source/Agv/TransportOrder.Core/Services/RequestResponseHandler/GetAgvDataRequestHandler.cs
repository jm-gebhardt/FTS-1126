using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv specification-related requests.
/// </summary>
[UsedImplicitly]
public class GetAgvDataRequestHandler :
    IConsumer<GetAgvData>,
    IConsumer<GetAgvsData>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="GetAgvDataRequestHandler"/> instance.
    /// </summary>
    public GetAgvDataRequestHandler(IAgvDb agvDb,
        IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvData"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    /// <exception cref="AgvDataNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgvData> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agvData = await _agvDb.Data.GetAgvDataAsync(correlationId);
        if (agvData == null)
        {
            throw new AgvDataNotFoundException(correlationId);
        }

        var agvDataMessageContent = _mapper.Map<AgvData>(agvData);
        await context.RespondAsync(new GotAgvData(agvDataMessageContent));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvsData"/> request.
    /// </summary>
    /// <exception cref="AgvsDataBatchQueryException"></exception>
    public async Task Consume(ConsumeContext<GetAgvsData> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        ICollection<Common.Messages.AgvManagement.AgvData> fetchedData = await _agvDb.Data.GetSelectedAgvDataAsync(selectedAgvs);
        if (fetchedData.Count != selectedAgvs.Count)
        {
            throw new AgvsDataBatchQueryException(selectedAgvs.Except(fetchedData.Select(p => p.CorrelationId)).ToList());
        }
        var agvDataList = _mapper.Map<List<AgvData>>(fetchedData);

        await context.RespondAsync(new GotAgvsData(agvDataList));
    }
}