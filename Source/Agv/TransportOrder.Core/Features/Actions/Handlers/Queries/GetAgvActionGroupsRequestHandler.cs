using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="???"></param>
        public GetAgvActionGroupsRequestHandler(IActionGroupRegistry actionGroupRegistry)
        {
            _actionGroupRegistry = actionGroupRegistry;
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
            
            var result = new GetAgvActionGroupsResult(actionGroups);
            
            await context.RespondAsync(result);
        }
    }
}


