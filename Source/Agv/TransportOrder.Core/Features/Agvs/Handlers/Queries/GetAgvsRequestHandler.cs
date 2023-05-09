using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgv request
    /// Gets all Agv from AgvRegistry sorted by AgvId ascending, returns IEnumerable of Agv
    /// </summary>
    [UsedImplicitly]
    public class GetAgvsRequestHandler : IConsumer<GetAgvsRequest>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvsRequestHandler(IAgvRegistry agvRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAgvsRequest> context)
        {
            IOrderedQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs().OrderBy(a => a.AgvId);
            List<AgvDto> agvDtos = agvs.Select(agv => _mapper.Map<AgvDto>(agv)).ToList();
            await context.RespondAsync(new GetAgvsResult(agvDtos));
        }
    }
}