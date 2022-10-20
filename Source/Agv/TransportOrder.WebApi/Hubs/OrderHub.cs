using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs
{    
    /// <summary>
    /// Interface wrapper for <see cref="IHubContext"/>&lt;<see cref="OrderHub"/>> for better usability and unit testing
    /// </summary>
    public interface IOrderHubContext
    {
        /// <summary>
        /// Broadcasts an order to all clients
        /// </summary>
        public Task SendOrderToAllClients(string method, IOrder order, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Wrapper for <see cref="IHubContext"/>&lt;<see cref="OrderHub"/>> for better usability and unit testing
    /// </summary>
    public class OrderHubContext : IOrderHubContext
    {
        private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of <see cref="OrderHubContext"/>
        /// </summary>
        public OrderHubContext(IHubContext<OrderHub> hubContext, IMapper mapper)
        {
            _hubContext = hubContext;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task SendOrderToAllClients(string method, IOrder order, CancellationToken cancellationToken)
        {
            string message = JsonSerializer.Serialize(_mapper.Map<Order>(order), _serializerOptions);
            await _hubContext.Clients.All.SendAsync(method, message, cancellationToken);
        }
    }

    /// <summary>
    /// Interface to define calls to the OrderHub from the client to the server
    /// </summary>
    [SignalRHub]
    public interface IOrderHub
    {

    }

    /// <summary>
    /// SignalR hub for order view
    /// </summary>
    public class OrderHub : Hub<IOrderHub>
    {
        /// <summary>
        /// Url path
        /// </summary>
        public const string UrlPath = "/hub/order";

        private readonly ILogger<OrderHub> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="OrderHub"/>
        /// </summary>
        public OrderHub(ILogger<OrderHub> logger)
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
