using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRSwaggerGen.Attributes;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs
{
    /// <summary>
    /// Interface wrapper for <see cref="IHubContext"/>&lt;<see cref="AgvHub"/>> for better usability and unit testing
    /// </summary>
    public interface IAgvHubContext
    {
        /// <summary>
        /// Broadcasts the status of an agv to all clients
        /// </summary>
        public Task SendAgvStateToAllClients(string method, IAgv agv, CancellationToken cancellationToken);

        /// <summary>
        /// Broadcasts the position of an agv to all clients
        /// </summary>
        public Task SendAgvPositionToAllClients(string method, AgvPositionStatus agvPosition, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Wrapper for <see cref="IHubContext"/>&lt;<see cref="AgvHub"/>> for better usability and unit testing
    /// </summary>
    public class AgvHubContext : IAgvHubContext
    {
        private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly IHubContext<AgvHub> _hubContext;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of <see cref="AgvHubContext"/>
        /// </summary>
        public AgvHubContext(IHubContext<AgvHub> hubContext, IMapper mapper)
        {
            _hubContext = hubContext;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task SendAgvStateToAllClients(string method, IAgv agv, CancellationToken cancellationToken)
        {
            string message = JsonSerializer.Serialize(_mapper.Map<AgvDto>(agv), _serializerOptions);
            await _hubContext.Clients.All.SendAsync(method, message, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SendAgvPositionToAllClients(string method, AgvPositionStatus agvPosition, CancellationToken cancellationToken)
        {
            string message = JsonSerializer.Serialize(agvPosition, _serializerOptions);
            await _hubContext.Clients.All.SendAsync(method, message, cancellationToken);
        }
    }

    /// <summary>
    /// Interface to define calls to the AgvHub from the client to the server
    /// </summary>
    [SignalRHub]
    public interface IAgvHub
    {

    }

    /// <summary>
    /// SignalR hub for agv view
    /// </summary>
    public class AgvHub : Hub<IAgvHub>
    {
        /// <summary>
        /// Url path
        /// </summary>
        public const string UrlPath = "/hub/agv";

        private readonly ILogger<AgvHub> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="AgvHub"/>
        /// </summary>
        public AgvHub(ILogger<AgvHub> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected. Connection ID: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        /// <inheritdoc/>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception == null) // Regular disconnect
            {
                _logger.LogInformation("Client disconnected. Connection ID: {ConnectionId}", Context.ConnectionId);
            }
            else // Disconnect caused by exception
            {
                _logger.LogWarning(exception, "Client disconnected. Connection ID: {ConnectionId}", Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
