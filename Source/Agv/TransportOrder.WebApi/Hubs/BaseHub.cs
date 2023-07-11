using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs
{
    /// <summary>
    /// Base SignalR hub providing connect and disconnect.
    /// </summary>
    public abstract class BaseHub<THub> : Hub<THub> where THub : class
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Base class for a SignalR hub.
        /// </summary>
        protected BaseHub(ILogger logger)
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
