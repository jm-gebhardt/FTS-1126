using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
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
        private readonly IActionGroupRegistry _actionGroupRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="actionGroupRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvsRequestHandler(IAgvRegistry agvRegistry, IActionGroupRegistry actionGroupRegistry, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _actionGroupRegistry = actionGroupRegistry;
            _mapper = mapper;
        }

        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAgvsRequest> context)
        {
            List<IAgv> agvs = _agvRegistry.GetAllAgvs().OrderBy(a => a.AgvId).ToList();
            IReadOnlyDictionary<Guid, ActionGroup?> actions = await _actionGroupRegistry.GetCurrentAssignedActionGroupsAsync(agvs.Select(a => a.CorrelationId));
            List<AgvDto> agvDtos = agvs.Select(agv => _mapper.Map<AgvDto>((agv, actions[agv.CorrelationId]))).ToList();
            await context.RespondAsync(new GetAgvsResult(agvDtos));
        }
    }
}