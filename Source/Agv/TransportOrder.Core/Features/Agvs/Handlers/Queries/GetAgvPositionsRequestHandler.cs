using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;
using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the <see cref="GetAgvPositionsRequest"/>
    /// Returns the positions for all agvs in the registry
    /// </summary>
    [UsedImplicitly]
    public class GetAgvPositionsRequestHandler : IConsumer<GetAgvPositionsRequest>
    {
        private readonly IAgvPositionRegistry _agvPositionRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetAgvPositionsRequestHandler"/>
        /// </summary>
        /// <param name="agvPositionRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvPositionsRequestHandler(IAgvPositionRegistry agvPositionRegistry, IMapper mapper)
        {
            _agvPositionRegistry = agvPositionRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// <inheritdoc cref="GetAgvPositionsRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAgvPositionsRequest> context)
        {
            List<AgvPositionStatusDto> agvPositionsDtos = _agvPositionRegistry.GetAllPositions().Select(agvPosition => _mapper.Map<AgvPositionStatusDto>(agvPosition)).ToList();
            await context.RespondAsync(new GetAgvPositionsResult(agvPositionsDtos));
        }
    }
}