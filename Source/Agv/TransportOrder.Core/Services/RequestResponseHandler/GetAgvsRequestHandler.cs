using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the GetAgv <see cref="GetAgvs"/>
/// Gets all Agv from AgvRepository sorted by AgvId ascending, returns IEnumerable of Agv
/// </summary>
[UsedImplicitly]
public class GetAgvsRequestHandler :
    IConsumer<GetAgv>,
    IConsumer<GetAgvs>
{
    private readonly IActionGroupRetrievalService _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly IAgvDb _agvDb;

    /// <summary>
    /// Initializes a new  <see cref="GetAgvsRequestHandler"/> instance.
    /// </summary>
    public GetAgvsRequestHandler(
        IActionGroupRetrievalService actionGroupRepository,
        IAgvDb agvDb,
        IMapper mapper
    )
    {
        _actionGroupRepository = actionGroupRepository;
        _mapper = mapper;
        _agvDb = agvDb;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvs"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        ICollection<Domain.Models.Agv.Agv> agvs;
        if (selectedAgvs.Count == 0)
        {
            agvs = await _agvDb.Agvs.GetAllAgvsAsync();
        }
        else
        {
            agvs = await _agvDb.Agvs.SelectExistingAgvsAsync(selectedAgvs.ToList());
        }

        List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();
        if (agvCorrelationIds.Count != selectedAgvs.Count && selectedAgvs.Count != 0)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvCorrelationIds).ToList());
        }

        Dictionary<Guid, Guid?> agvLastNodeIds = agvs.ToDictionary(a => a.CorrelationId, a => a.AgvStatus?.LastNodeId);

        Task<IReadOnlyDictionary<Guid, GraphNode?>> agvLastNodesTask = _agvDb.Graphs.GetLastNodesForAgvsAsync(agvLastNodeIds);

        Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> actionGroupsTask = _actionGroupRepository.GetCurrentActionGroupsWithNodesAsync(agvCorrelationIds);
        Task<IReadOnlyList<AgvError>> errorsTask = _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationIds);
        Task<Dictionary<Guid, MaintenanceLog?>> maintenanceTask = _agvDb.MaintenanceLogs.GetLatestMaintenanceLogsForAgvsAsync(agvCorrelationIds);
        await Task.WhenAll(actionGroupsTask, errorsTask, agvLastNodesTask, maintenanceTask);

        Dictionary<Guid, List<AgvError>> allAgvErrors = errorsTask.Result
            .GroupBy(e => e.AgvCorrelationId)
            .Where(e => e.Any())
            .ToDictionary(e => e.Key, e => e.ToList());

        List<Agv> agvsMessageContent = agvs.Select(agv =>
        {
            var agvMessageContent = _mapper.Map<Agv>(agv, opts =>
            {
                opts.Items[nameof(Agv.CurrentActionGroup)] = actionGroupsTask.Result[agv.CorrelationId];
                opts.Items[nameof(Agv.Errors)] = allAgvErrors.GetValueOrDefault(agv.CorrelationId, []);
                opts.Items[nameof(Agv.LastNode)] = agvLastNodesTask.Result[agv.CorrelationId];
                opts.Items[nameof(Agv.LastMaintenance)] = maintenanceTask.Result.GetValueOrDefault(agv.CorrelationId);
            });

            return agvMessageContent;
        }).ToList();
        await context.RespondAsync(new GotAgvs(agvsMessageContent));
    }

    /// <summary>
    /// Handles the <see cref="GetAgv"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        Task<ActionGroupReadModel?> agTask = _actionGroupRepository.GetCurrentAgvActionGroupWithNodesAsync(agvCorrelationId);
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        Guid? lastNodeId = agv.AgvStatus?.LastNodeId;
        Task<GraphNode?> lastNodeTask = Task.FromResult<GraphNode?>(null);
        Task<MaintenanceLog?> maintenanceLogTask = _agvDb.MaintenanceLogs.GetLatestMaintenanceLogForAgvAsync(agvCorrelationId);

        if (lastNodeId != null)
        {
            lastNodeTask = _agvDb.Graphs.TryGetNodeByIdAsync(lastNodeId.Value);
        }

        await Task.WhenAll(lastNodeTask, agTask, maintenanceLogTask);
        var lastNode = lastNodeTask.Result;
        var actionGroup = agTask.Result;
        var maintenanceLog = maintenanceLogTask.Result;

        IReadOnlyList<AgvError> agvErrors = await _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);

        var agvMessageContent = _mapper.Map<Agv>(agv, opts =>
        {
            opts.Items[nameof(Agv.CurrentActionGroup)] = actionGroup;
            opts.Items[nameof(Agv.Errors)] = agvErrors;
            opts.Items[nameof(Agv.LastNode)] = lastNode;
            opts.Items[nameof(Agv.LastMaintenance)] = maintenanceLog;
        });

        await context.RespondAsync(new GotAgv(agvMessageContent));
    }
}