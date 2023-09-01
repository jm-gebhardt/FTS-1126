using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRSwaggerGen.Attributes;
using TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;

/// <summary>
/// Action client methods
/// </summary>
[SignalRHub(path: ActionHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IActionHubClient
{
    /// <summary>
    /// ActionGroup changed
    /// </summary>
    /// <param name="actionGroup"></param>
    [SignalRMethod("ActionGroupChanged")]
    public Task ActionGroupChanged(ActionGroupDto actionGroup);       
}

/// <summary>
/// SignalR hub for getting updates for action changes
/// </summary>
public class ActionHub : BaseHub<IActionHubClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/action";

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
    public Task SendActionGroupStateToAllClients(ActionGroupReadModel actionGroup);
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
    public async Task SendActionGroupStateToAllClients(ActionGroupReadModel actionGroup)
    {
        var actionGroupDto = _mapper.Map<ActionGroupDto>(actionGroup);
        await _hubContext.Clients.All.ActionGroupChanged(actionGroupDto);
    }
}
