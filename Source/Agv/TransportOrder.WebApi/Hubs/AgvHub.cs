using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs
{
    /// <summary>
    /// AGV client methods
    /// </summary>
    public interface IAgvHubClient
    {
        /// <summary>
        /// AGV state changed
        /// </summary>
        /// <param name="agv"></param>
        [SignalRMethod("AgvDiscovered")]
        public Task AgvDiscovered(AgvDto agv);

        /// <summary>
        /// AGV state changed
        /// </summary>
        /// <param name="agv"></param>
        [SignalRMethod("AgvChanged")]
        public Task AgvChanged(AgvDto agv);

        /// <summary>
        /// AGV position changed
        /// </summary>
        /// <param name="position"></param>
        [SignalRMethod("AgvPositionChanged")]
        public Task AgvPositionChanged(AgvPositionStatusDto position);
    }

    /// <summary>
    /// SignalR hub for agv view
    /// </summary>
    public class AgvHub : BaseHub<IAgvHubClient>
    {
        /// <summary>
        /// Url path
        /// </summary>
        public static string UrlPath => "/hub/agv";

        /// <summary>
        /// Initializes a new instance of <see cref="AgvHub"/>
        /// </summary>
        public AgvHub(ILogger<AgvHub> logger) : base(logger)
        {

        }
    }

    /// <summary>
    /// Interface wrapper for <see cref="IHubContext{AgvHub}"/>
    /// </summary>
    public interface IAgvHubContext
    {
        /// <summary>
        /// Broadcasts the status of an agv to all clients
        /// </summary>
        public Task SendAgvStateToAllClients(IAgv agv, bool isInitialState);

        /// <summary>
        /// Broadcasts the position of an agv to all clients
        /// </summary>
        public Task SendAgvPositionToAllClients(AgvPositionStatus agvPosition);
    }

    /// <summary>
    /// Wrapper for <see cref="IHubContext{AgvHub}"/>
    /// </summary>
    public class AgvHubContext : IAgvHubContext
    {
        private readonly IHubContext<AgvHub, IAgvHubClient> _hubContext;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of <see cref="AgvHubContext"/>
        /// </summary>
        public AgvHubContext(IHubContext<AgvHub, IAgvHubClient> hubContext, IMapper mapper)
        {
            _hubContext = hubContext;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task SendAgvStateToAllClients(IAgv agv, bool isInitialState)
        {
            // Combine Agv and AgvStatus into AgvStatusDto
            var agvDto = _mapper.Map<AgvDto>(agv);

            if (isInitialState)
            {
                await _hubContext.Clients.All.AgvDiscovered(agvDto);
            }
            else
            {
                await _hubContext.Clients.All.AgvChanged(agvDto);
            }
        }

        /// <inheritdoc/>
        public async Task SendAgvPositionToAllClients(AgvPositionStatus agvPosition)
        {
            var agvPositionDto = _mapper.Map<AgvPositionStatusDto>(agvPosition);
            await _hubContext.Clients.All.AgvPositionChanged(agvPositionDto);
        }
    }
}
