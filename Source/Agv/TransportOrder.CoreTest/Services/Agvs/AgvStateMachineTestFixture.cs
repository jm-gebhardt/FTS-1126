using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs;

public class AgvStateMachineTestFixture: BaseStateMachineTestFixture<AgvStateMachine, AgvSaga>
{
    public Mock<IAgvDataRegistry> MockAgvDataRegistry { get; } = new();

    private Dictionary<Guid, AgvData> AgvData { get; } = new();

    public Mock<IMapRegistry> MockMapRegistry { get; } = new();

    private Mock<IGraphRegistry> MockGraphRegistry { get; } = new();

    public Mock<IStateObserver<AgvSaga>> MockObserver { get; } = new();

    private Mock<ISettingsProvider<AgvSettings>> MockSettingsProvider { get; } = new();

    private AgvSettings? AgvSettings { get; }

    public AgvStateMachineTestFixture(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        AgvSettings? settings = null,
        TimeSpan? inactivityTimeout = null) 
         : base(services, busRegistrationConfigurator, inMemoryConfiguration, inactivityTimeout)
        {
            ConfigureServices();
            AgvSettings = settings;
            SetupSettingsRegistry();
            SetupAgvDataRegistry();
            Build();
        }

    private void ConfigureServices()
    {
        ServiceCollection.AddSingleton<IAgvDataRegistry>(_ => MockAgvDataRegistry.Object);
        ServiceCollection.AddSingleton<IMapRegistry>(_ => MockMapRegistry.Object);
        ServiceCollection.AddSingleton<IGraphRegistry>(_ => MockGraphRegistry.Object);
        ServiceCollection.AddSingleton<IStateObserver<AgvSaga>>(_ => MockObserver.Object);
        
        ServiceCollection.AddSingleton(_ => new ProductionOptions()
        {
            ApplicationName = null,
            AutoAccept = true
        });
        
        ServiceCollection.AddSingleton(MockSettingsProvider.Object);
    }

    private void SetupSettingsRegistry()
    {
        MockSettingsProvider.Setup(m => m.GetSettings()).Returns(() => AgvSettings ?? new AgvSettings());
    }

    private void SetupAgvDataRegistry()
    {
        MockAgvDataRegistry.Setup(x => x.Exists(It.IsAny<Guid>())).Returns<Guid>(correlationId => AgvData.ContainsKey(correlationId));
        MockAgvDataRegistry.Setup(x => x.GetAgvData(It.IsAny<Guid>()))
            .Returns<Guid>(correlationId =>
            {
                AgvData.TryGetValue(correlationId, out var agvData);
                return agvData;
            });
        MockAgvDataRegistry.Setup(x => x.AddOrUpdate(It.IsAny<AgvData>()))
            .Returns<AgvData>(agvData =>
            {
                AgvData[agvData.CorrelationId] = agvData;
                return true;
            });
    }

    /// <summary>
    /// Create a standardized AgvSaga for testing purposes
    /// </summary>
    /// <param name="state"></param>
    /// <param name="correlationId"></param>
    /// <param name="hasGraph"></param>
    public AgvSaga CreateTestAgvSaga(string state, Guid? correlationId = null, bool hasGraph = true)
    {
        correlationId ??= Guid.NewGuid();
        string agvId = correlationId.ToString()!;
        
        var agvGraph = hasGraph? CreateTestAgvGraph() : null;
        var agvStatus = new AgvStatus(
            correlationId.Value, 
            null, 
            true, 
            true, 
            10, 
            true, 
            true, 
            lastNodeId: agvGraph?.Nodes.First().Id
            ); 
        
        var agvSaga = new AgvSaga(
            correlationId.Value,
            agvId,
            currentState: state,
            agvStatus: agvStatus,
            agvGraph: agvGraph
        );
        return agvSaga;
    }
    
    /// <summary>
    /// Create a standardized AgvGraph for testing purposes
    /// </summary>
    public AgvGraph CreateTestAgvGraph()
    {
        const string mapName = "Map#1";
        var agvGraph = new AgvGraph(Guid.Empty,
            new[]
            {
                new AgvGraph.Node("node01"){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
                new AgvGraph.Node("node02"){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f}
            }, new[]
            {
                new AgvGraph.Edge("node01", "node02"){ Metric = 4f}
            },
            mapName.ToGuid(),
            mapName);

        return agvGraph;
    }

    /// <summary>
    /// Create a standardized AgvMap for testing purposes
    /// </summary>
    public AgvMap CreateTestAgvMap()
    {
        const string mapId = "basicMap";
        const string imageData = "bGlnaHQgd29yay4=";
        const float resolution = 1;
        const float xOffset = 1;
        const float yOffset = 1;
        var modificationDate = DateTime.UtcNow;

        return new AgvMap(mapId.ToGuid(), mapId, modificationDate, imageData, resolution, xOffset, yOffset);
    }
    
    public async Task<AgvSaga> TransitionStateOnRequest<TRequest, TExpectedResponse, TUnexpectedResponse>(TRequest request, AgvSaga agvSaga, string resultingState) 
        where TRequest : class, CorrelatedBy<Guid>
        where TExpectedResponse : class
        where TUnexpectedResponse : class
    {
        IRequestClient<TRequest>? client = TestHarness.GetRequestClient<TRequest>();
        Response response = await client.GetResponse<TExpectedResponse, TUnexpectedResponse>(request);
        response.Message.ShouldBeAssignableTo(typeof(TExpectedResponse));

        var saga = SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        saga.CurrentState.ShouldBe(resultingState);
        return saga;
    }

    public async Task TransitionStateWhenMessageReceived<TRequest>(
        TRequest message,
        AgvSaga agvSaga,
        string resultingState,
        bool assertAreEqual = true)
        where TRequest : class, CorrelatedBy<Guid>
    {
        await TestHarness.Bus.Publish(message);

        (await TestHarness.Published.Any<TRequest>()).ShouldBeTrue();
        (await SagaHarness.Consumed.Any<TRequest>()).ShouldBeTrue();

        var saga = SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
        if (assertAreEqual)
        {
            saga.CurrentState.ShouldBe(resultingState);
        }
        else
        {
            saga.CurrentState.ShouldNotBe(resultingState);
        }
    }
}