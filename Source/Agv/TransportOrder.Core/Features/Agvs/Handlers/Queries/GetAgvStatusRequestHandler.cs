using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for GetAgvStatus request
    /// Retrieves status of Agv 
    /// </summary>
    [UsedImplicitly]
    public class GetAgvStatusRequestHandler : IConsumer<GetAgvStatusRequest>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetAgvStatusRequestHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvStatusRequestHandler(IAgvRegistry agvRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// <inheritdoc cref="GetAgvStatusRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        /// <exception cref="AgvStatusNotFoundException"></exception>
        public async Task Consume(ConsumeContext<GetAgvStatusRequest> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetAgv(request.CorrelationId);
            if (agv == null)
            {
                throw new AgvNotFoundException(request.CorrelationId);
            }

            agv.RequireAgvStatus();

            var agvStateDto = _mapper.Map<AgvStateDto>(agv);

            await context.RespondAsync(new GetAgvStateResult(agvStateDto));
        }
    }
}