using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs
{
    /// <summary>
    /// Action client methods
    /// </summary>
    public interface IActionHubClient
    {
        /// <summary>
        /// ActionGroup changed
        /// </summary>
        /// <param name="agv"></param>
        [SignalRMethod("ActionGroupChanged")]
        public Task ActionGroupChanged(ActionGroupDto agv);       
    }

    /// <summary>
    /// SignalR hub for getting updates for action changes
    /// </summary>
    public class ActionHub : BaseHub<IActionHubClient>
    {
        /// <summary>
        /// Url path
        /// </summary>
        public static string UrlPath => "/hub/action";

        /// <summary>
        /// Initializes a new instance of <see cref="ActionHub"/>
        /// </summary>
        public ActionHub(ILogger<ActionHub> logger) : base(logger)
        {

        }
    }

    /// <summary>
    /// Interface wrapper for <see cref="IHubContext{ActionHub}"/>
    /// </summary>
    public interface IActionHubContext
    {
        /// <summary>
        /// Broadcasts the state of action groups
        /// </summary>
        public Task SendActionGroupStateToAllClients(ActionGroup actionGroup);
    }

    /// <summary>
    /// Wrapper for <see cref="IHubContext{ActionHub}"/>
    /// </summary>
    public class ActionHubContext : IActionHubContext
    {
        private readonly IHubContext<ActionHub, IActionHubClient> _hubContext;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of <see cref="ActionHubContext"/>
        /// </summary>
        public ActionHubContext(IHubContext<ActionHub, IActionHubClient> hubContext, IMapper mapper)
        {
            _hubContext = hubContext;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task SendActionGroupStateToAllClients(ActionGroup actionGroup)
        {
            var actionGroupDto = _mapper.Map<ActionGroupDto>(actionGroup);
            await _hubContext.Clients.All.ActionGroupChanged(actionGroupDto);
        }
    }
}
