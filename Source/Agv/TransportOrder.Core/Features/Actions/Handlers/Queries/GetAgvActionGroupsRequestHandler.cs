using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgvActionGroups request
    /// </summary>
    public class GetAgvActionGroupsRequestHandler: IConsumer<GetAgvActionGroupsRequest>
    {
        private readonly IActionGroupRegistry _actionGroupRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="actionGroupRegistry"></param>
        /// <param name="mapper"></param>
        public GetAgvActionGroupsRequestHandler(IActionGroupRegistry actionGroupRegistry, IMapper mapper)
        {
            _actionGroupRegistry = actionGroupRegistry;
            _mapper = mapper;
        }
        
        
        /// <summary>
        /// <inheritdoc cref="GetAgvActionGroupsRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task Consume(ConsumeContext<GetAgvActionGroupsRequest> context)
        {
            IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRegistry.GetActionGroupsAsync(context.Message.States, context.Message.FromDate, context.Message.ToDate, context.Message.Limit);
            
            var result = new GetAgvActionGroupsResult(actionGroups.Select(g => _mapper.Map<ActionGroupDto>(g)).ToList());
            
            await context.RespondAsync(result);
        }
    }
}


