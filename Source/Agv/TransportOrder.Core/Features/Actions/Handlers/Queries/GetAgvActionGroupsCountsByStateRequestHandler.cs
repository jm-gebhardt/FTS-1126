using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgvActionGroups request
    /// </summary>
    [PublicAPI]
    public class GetAgvActionGroupCountsByStateRequestHandler : IConsumer<GetAgvActionGroupCountsByStateRequest>
    {
        private readonly IActionGroupRegistry _actionGroupRegistry;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="actionGroupRegistry"></param>
        public GetAgvActionGroupCountsByStateRequestHandler(IActionGroupRegistry actionGroupRegistry)
        {
            _actionGroupRegistry = actionGroupRegistry;
        }

        /// <summary>
        /// Consumer for the GetAgvActionGroupCountsByStateRequest
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAgvActionGroupCountsByStateRequest> context)
        {
            AgvGroupCountByStateDto[] actionGroupStateCounts = await Task.WhenAll(Enum.GetValues<ActionGroupState>().Select(state =>
                _actionGroupRegistry.GetActionGroupsCountAsync(new[] { state })
                    .ContinueWith(actionGroupCount => new AgvGroupCountByStateDto(state, actionGroupCount.Result))));

            await context.RespondAsync(new GetAgvActionGroupCountsByStateResult(actionGroupStateCounts));
        }
    }
}