using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for GetAgvData request
    /// Gets agv from registry by correlationId returns AgvData
    /// </summary>
    [UsedImplicitly]
    public class GetAgvDataRequestHandler : IConsumer<GetAgvDataRequest>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetAgvDataRequestHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvDataRequestHandler(IAgvRegistry agvRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// <inheritdoc cref="GetAgvDataRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        /// <exception cref="AgvDataNotFoundException"></exception>
        public async Task Consume(ConsumeContext<GetAgvDataRequest> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetAgv(request.CorrelationId);
            if (agv == null)
            {
                throw new AgvNotFoundException(request.CorrelationId);
            }

            if (agv.AgvData == null)
            {
                throw new AgvDataNotFoundException(agv);
            }

            var agvDataDto = _mapper.Map<AgvDataDto>(agv.AgvData);
            await context.RespondAsync(new GetAgvDataResult(agvDataDto));
        }
    }
}