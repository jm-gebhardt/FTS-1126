using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.ActionProfile;
using AgvData = Gebhardt.Acc.Common.Domain.Models.AgvData;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using AgvPositionProfile = Gebhardt.Acc.TransportOrder.WebApi.Profiles.AgvPositionProfile;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Agv;

public class AgvsControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, AgvsController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    public string[] AgvSerialNumbers { get; } = {"AGV001", "AGV002", "AGV003"};

    // ReSharper disable once MemberCanBePrivate.Global
    public string[] Orders { get; } = {"ORDER001", "ORDER002", ""};

    public AgvState[] States { get; } = {AgvState.Idle, AgvState.Busy, AgvState.Discovered};

    private string[] Ips { get; } = {"127.0.0.1", "127.0.0.2", "127.0.0.3"};

    private readonly AgvSettingsReadModel _agvSettingsReadModel = EfAgvSettingsProvider.DefaultSettings;

    private Mock<IAgvDb> AgvDb { get; }

    private Mock<IActionGroupRepository> ActionGroupRepository { get; }

    private Mock<IActionGroupRetrievalService> ActionGroupRetrievalService { get; }

    private Mock<IRequestClient<QueueActionGroup>> QueueActionGroupRequestClient { get; set; }

    private readonly GraphNode[] _nodes =
    {
        new("node00") {X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node10") {X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node01") {X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node11") {X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint}
    };

    private IRequestClient<GetAgvs>? _getAgvsRequestClient;
    private IRequestClient<GetCurrentAgvRoutes>? _getCurrentAgvRoutesRequestClient;
    private IRequestClient<GetAgvPositions>? _getAgvPositionsRequestClient;
    private IRequestClient<GetAgvGraphs>? _getAgvGraphsRequestClient;
    private IRequestClient<GetAgvsData>? _getAgvGetAgvsDataRequestClient;
    private IRequestClient<GetAgvsBattery>? _getAgvGetAgvsBatteryRequestClient;
    private IRequestClient<GetActiveErrorsForAgvs>? _getActiveErrorsRequestClient;
    private IRequestClient<GetErrorHistoryForAgvs>? _getErrorHistoryRequestClient;
    private IRequestClient<GetAgvsSettings>? _getAgvsSettingsRequestClient;
    private IRequestClient<GetCargosOfAgvs>? _getCargosOfAgvsRequestClient;
    private IRequestClient<GetAgvsHasCargo>? _getAgvsHasCargoRequestClient;
    private readonly IAgvPositionRepository _agvPositionRepository;

    public Common.Messages.ActionGroups.ActionGroup? SentQueueActionGroup
    {
        [UsedImplicitly]
        get;
        private set;
    }

    public AgvsControllerTestFixture()
    {
        AgvDb = new Mock<IAgvDb>();

        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        ActionGroupRetrievalService = new Mock<IActionGroupRetrievalService>();
        ActionGroupRetrievalService.Setup(r => r.GetCurrentActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (ActionGroupReadModel?) null));
        _agvPositionRepository = GetAgvPositionRepository().Object;

        ActionGroupRepository = new Mock<IActionGroupRepository>();
        ActionGroupRepository.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (TransportOrder.Domain.Models.ActionGroup.ActionGroup?) null));
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        ActionGroupRepository.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (TransportOrder.Domain.Models.ActionGroup.ActionGroup?) null));
        AgvDb.Setup(x => x.Graphs.GetLastNodesForAgvsAsync(It.IsAny<Dictionary<Guid, string?>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (GraphNode?) null));
        AgvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(_agvSettingsReadModel);
    }

    [UsedImplicitly]
    public AgvsControllerTestFixture<TRequest, TRequestHandler> SetupAgvDbWithDefaultData()
    {
        var data = new Dictionary<Guid, AgvData>();
        List<TransportOrder.Domain.Models.Agv.Agv> agvs = new();
        for (int i = 0; i < AgvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = AgvSerialNumbers[i];
            string orderId = Orders[i];
            var state = States[i];
            string ip = Ips[i];
            var correlationId = GetGuid(agvSerialNumber);
            var agvStatus = agvSerialNumber == "AGV003" ? null : new AgvStatus(correlationId, orderId, false, true, 10, true, true, lastNodeId: "Node");
            var agvData = AgvDataGenerator.GenerateAgvData(ipV4Address: ip);
            var agv = AgvGenerator.GenerateRawAgv(correlationId, agvSerialNumber, agvStatus);
            agv = AgvGenerator.SetAgvState(state, agv, false);

            agvs.Add(agv);
            data[correlationId] = agvData;
        }

        AgvDb.AddAgvs(agvs);

        AgvDb.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => data.ContainsKey(correlationId));
        AgvDb.AddAgvData(correlationId =>
        {
            data.TryGetValue(correlationId, out var agvData);
            return agvData;
        });
        AgvDb.Setup(x => x.Data.AddOrUpdateAsync(It.IsAny<AgvData>()))
            .Callback((AgvData agvData) => { data[agvData.CorrelationId] = agvData; });

        QueueActionGroupRequestClient = new Mock<IRequestClient<QueueActionGroup>>();
        QueueActionGroupRequestClient.Setup(x => x.GetResponse<Success>(It.IsAny<QueueActionGroup>(), default, default))
            .Callback((QueueActionGroup q, CancellationToken _, RequestTimeout _) => { SentQueueActionGroup = q.ActionGroup; });

        AgvDb.Setup(x => x.Graphs.GetLastNodesForAgvsAsync(It.IsAny<Dictionary<Guid, string?>>())).ReturnsAsync(() => AgvSerialNumbers.ToDictionary(GetGuid, _ => (GraphNode?) null));
        AgvDb.Setup(x => x.AgvSettings.GetSettingsGroupForAgv(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(new AgvSettingsGroup(GroupType.All));
        AgvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
        AgvDb.Setup(x => x.AgvSettings.AddOrUpdateSettingsForAgvGroup(It.IsAny<AgvSettingsReadModel>(), It.IsAny<GroupType>(), It.IsAny<string>())).Verifiable();
        return this;
    }

    private static Guid GetGuid(string agvSerialNumber) => agvSerialNumber.ToGuid();

    private Mock<IAgvPositionRepository> GetAgvPositionRepository()
    {
        var repository = new Mock<IAgvPositionRepository>();
        var agvPositions = new List<AgvPositionStatus>();

        for (int i = 0; i < AgvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = AgvSerialNumbers[i];
            var correlationId = GetGuid(agvSerialNumber);
            var node = _nodes[i];

            agvPositions.Add(new AgvPositionStatus(correlationId, agvSerialNumber, new AgvPosition(true, node.X, node.Y, node.Z, 0.5f)));
        }

        repository.Setup(x => x.GetAllPositions()).Returns(agvPositions);
        repository.Setup(x => x.GetPosition(It.IsAny<Guid>())).Returns<Guid>(correlationId => { return agvPositions.FirstOrDefault(agvPosition => agvPosition.CorrelationId == correlationId); });

        return repository;
    }

    public AgvsControllerTestFixture<TRequest, TRequestHandler> WithHasCargo(Dictionary<Guid, bool> hasCargoById)
    {
        AgvDb.Setup(x => x.Cargos.HaveAgvsCargoAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(hasCargoById);
        AgvDb.Setup(x => x.Agvs.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Verifiable();
        return this;
    }

    /// <inheritdoc/>
    protected override AgvsController GetController()
    {
        _getAgvsRequestClient ??= GetRequestClient<GetAgvs>();
        _getCurrentAgvRoutesRequestClient ??= GetRequestClient<GetCurrentAgvRoutes>();
        _getAgvPositionsRequestClient ??= GetRequestClient<GetAgvPositions>();
        _getAgvGraphsRequestClient ??= GetRequestClient<GetAgvGraphs>();
        _getAgvGetAgvsDataRequestClient ??= GetRequestClient<GetAgvsData>();
        _getAgvGetAgvsBatteryRequestClient ??= GetRequestClient<GetAgvsBattery>();
        _getActiveErrorsRequestClient ??= GetRequestClient<GetActiveErrorsForAgvs>();
        _getErrorHistoryRequestClient ??= GetRequestClient<GetErrorHistoryForAgvs>();
        _getCargosOfAgvsRequestClient ??= GetRequestClient<GetCargosOfAgvs>();
        _getAgvsSettingsRequestClient ??= GetRequestClient<GetAgvsSettings>();
        _getAgvsHasCargoRequestClient ??= GetRequestClient<GetAgvsHasCargo>();
        IRequestClient<UpdateDefaultAgvSettings> updateDefaultSettingsRequestClient = GetRequestClient<UpdateDefaultAgvSettings>();
        IRequestClient<UpdateAgvsSettings> updateAgvsSettingsRequestClient = GetRequestClient<UpdateAgvsSettings>();
        IRequestClient<ResetAgvsSettingToDefault> resetAgvsSettingsRequestClient = GetRequestClient<ResetAgvsSettingToDefault>();

        var controller = new AgvsController(Mapper,
            _getAgvsRequestClient,
            _getCurrentAgvRoutesRequestClient,
            _getAgvPositionsRequestClient,
            _getAgvGraphsRequestClient,
            _getAgvGetAgvsDataRequestClient,
            _getAgvGetAgvsBatteryRequestClient,
            _getActiveErrorsRequestClient,
            _getErrorHistoryRequestClient,
            _getAgvsSettingsRequestClient,
            _getCargosOfAgvsRequestClient,
            _getAgvsHasCargoRequestClient,
            updateDefaultSettingsRequestClient,
            updateAgvsSettingsRequestClient,
            resetAgvsSettingsRequestClient
        )
        {
            ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor()))
        };

        return controller;
    }
    
    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new AgvProfile(), 
        new Gebhardt.Acc.TransportOrder.Messages.Profiles.AgvProfile(),
        new AgvStatusProfile(),
        new AgvSettingsProfile(),
        new AgvGraphProfile(),
        new Gebhardt.Acc.TransportOrder.Messages.Profiles.AgvGraphProfile(),
        new AgvPositionProfile(),
        new AgvErrorProfile(),
        new Gebhardt.Acc.TransportOrder.Messages.Profiles.AgvErrorProfile(),
        new Gebhardt.Acc.TransportOrder.WebApi.Profiles.ActionProfile(),
        new ActionProfile(),
        new CargoProfile()
    ];

    public AgvsControllerTestFixture<TRequest, TRequestHandler> WithActiveAgvErrors(IList<AgvError> agvErrors)
    {
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(agvErrors);
        AgvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(agvErrors);
        return this;
    }

    public new AgvsControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => AgvDb.Object);
            sc.AddTransient(_ => ActionGroupRetrievalService.Object);
            sc.AddTransient(_ => AgvDb.Object);
            sc.AddTransient(_ => _agvPositionRepository);
            sc.AddTransient(_ => new Mock<IBus>().Object);
            sc.AddTransient(_ => new Mock<IAgvPositionRepository>().Object);
            sc.AddTransient(_ => ActionGroupRepository.Object);
            sc.AddTransient(_ => QueueActionGroupRequestClient.Object);
        });

        base.Build();

        return this;
    }
}