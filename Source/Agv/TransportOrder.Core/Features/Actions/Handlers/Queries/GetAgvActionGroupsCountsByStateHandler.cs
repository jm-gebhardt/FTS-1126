using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgvActionGroups request
    /// </summary>
    public class GetAgvActionGroupCountsByStateHandler: IConsumer<GetAgvActionGroupCountsByStateRequest>
    {
        private readonly IActionGroupRegistry _actionGroupRegistry;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="actionGroupRegistry"></param>
        public GetAgvActionGroupCountsByStateHandler(IActionGroupRegistry actionGroupRegistry)
        {
            _actionGroupRegistry = actionGroupRegistry;
        }
        
        /// <summary>
        /// Consumer for the GetAgvActionGroupCountsByStateRequest
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<GetAgvActionGroupCountsByStateRequest> context)
        {
            
            return context.RespondAsync(
                new GetAgvActionGroupCountsByStateResult(
                    Enum.GetValues<ActionGroupState>().Select(state => new AgvGroupCountByStateDto(state, 123))
            ));
        }
    }
}


