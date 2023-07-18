using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetAgv request
    /// Gets all Agv from AgvRegistry sorted by AgvId ascending, returns IEnumerable of Agv
    /// </summary>
    [UsedImplicitly]
    public class GetAgvsRequestHandler : IConsumer<GetAgvsRequest>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IActionGroupRegistry _actionGroupRegistry;
        private readonly IMapper _mapper;
        private readonly IAgvErrorRegistry _agvErrorRegistry;

        /// <summary>
        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="actionGroupRegistry"></param>
        /// <param name="mapper"></param>
        /// <param name="agvErrorRegistry"></param>
        public GetAgvsRequestHandler(IAgvRegistry agvRegistry, IActionGroupRegistry actionGroupRegistry, IMapper mapper, IAgvErrorRegistry agvErrorRegistry)
        {
            _agvRegistry = agvRegistry;
            _actionGroupRegistry = actionGroupRegistry;
            _mapper = mapper;
            _agvErrorRegistry = agvErrorRegistry;
        }

        /// <inheritdoc cref="GetAgvsRequestHandler"/>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetAgvsRequest> context)
        {
            IQueryable<IAgv> query = _agvRegistry.GetAllAgvs();
            if (context.Message.CorrelationIds.Any()) // Optionally filter for specific correlation ids
            {
                query = query.Where(a => context.Message.CorrelationIds.Contains(a.CorrelationId));
            }

            List<IAgv> agvs = query.OrderBy(a => a.AgvId).ToList();
            List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();

            Task<IReadOnlyDictionary<Guid, ActionGroup?>> actionGroupsTask = _actionGroupRegistry.GetCurrentAssignedActionGroupsAsync(agvCorrelationIds);
            Task<IList<AgvError>> errorsTask = _agvErrorRegistry.GetActiveErrorsAsync(agvCorrelationIds);

            await Task.WhenAll(actionGroupsTask, errorsTask);

            Dictionary<Guid, List<AgvError>> allAgvErrors = errorsTask.Result
                .GroupBy(e => e.AgvCorrelationId)
                .ToDictionary(e => e.Key, e => e.ToList());

            List<AgvDto> agvDtos = agvs.Select(agv =>
            {
                var agvDto = _mapper.Map<AgvDto>(agv);
                _mapper.Map(actionGroupsTask.Result[agv.CorrelationId], agvDto);

                if (allAgvErrors.TryGetValue(agv.CorrelationId, out List<AgvError>? agvErrors) && agvErrors.Any())
                {
                    _mapper.Map(agvErrors, agvDto);
                }
                return agvDto;
            }).ToList();
            await context.RespondAsync(new GetAgvsResult(agvDtos));
        }
    }
}