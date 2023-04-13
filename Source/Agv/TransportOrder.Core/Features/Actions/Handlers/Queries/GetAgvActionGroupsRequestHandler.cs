using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgvActionGroups request
    /// </summary>
    public class GetAgvActionGroupsRequestHandler: IConsumer<GetAgvActionGroupsRequest>
    {
        /// <summary>
        /// <inheritdoc cref="GetAgvActionGroupsRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task Consume(ConsumeContext<GetAgvActionGroupsRequest> context)
        {
            Guid groupId0 = Guid.NewGuid();
            var result = new GetAgvActionGroupsResult(new List<AgvActionGroupDto>()
            {
                new(
                    groupId0, 
                    ActionState.Created, 
                    DateTime.MinValue, 
                    DateTime.MinValue, 
                    DateTime.MinValue,
                    Guid.NewGuid(),
                    new []
                    {
                        new AgvActionDto(Guid.NewGuid(), groupId0, 0, null, null, ActionState.Created, ActionType.Charge, "n0", new []{"n0"})
                    })
            });
            
            await context.RespondAsync(result);
        }
    }
}


