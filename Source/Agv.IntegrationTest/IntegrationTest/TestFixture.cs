using Gebhardt.Acc.IntegrationTest.Agv;
using Gebhardt.Acc.IntegrationTest.Api;
using Gebhardt.Acc.IntegrationTest.Helpers;
using Gebhardt.Acc.IntegrationTest.Mqtt;
using Gebhardt.Acc.IntegrationTest.Utils;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Gebhardt.Acc.IntegrationTest;

internal class TestFixture
{
    private readonly string _dockerRegistryUrl = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryUrl);
    private readonly string _dockerRegistryRepository = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryRepository);
    private readonly string _dockerRegistryUser = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryUser);
    private readonly string _dockerRegistryPassword = EnvHelper.GetRequiredEnvVar(EnvVars.DockerRegistryPassword);
    private readonly string _mqttSamplePath = EnvHelper.GetRequiredEnvVar(EnvVars.MqttSamplePath);

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger _logger;
    private DockerCompose _dockerCompose;
    private TransportOrder _transportOrder = null!;
    private TransportOrderClient _api = null!;
    private bool _withVirtualAgv = false;

    public TestFixture()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _dockerCompose = new DockerCompose([EnvVars.ComposeFileEnvVar]);
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        _logger = loggerFactory.CreateLogger<CompleteOrderTest>();
    }

    public TestFixture WithVirtualAgv()
    {
        _dockerCompose = new DockerCompose([EnvVars.ComposeFileEnvVar, EnvVars.ComposeFileEnvVarVirtualAgv], _dockerRegistryUrl,
            _dockerRegistryRepository, _dockerRegistryUser, _dockerRegistryPassword, _dockerCompose.EnvFile);
        _withVirtualAgv = true;
        return this;
    }

    public async Task StartAsync()
    {
        await _dockerCompose.InitializeAsync();
        _api = TransportOrderClient.GetClient(_dockerCompose.EnvFile, _logger);
        _transportOrder = new TransportOrder(_api, _logger);

        (await _dockerCompose.AreRemovedAsync()).ShouldBeTrue();

        if (_withVirtualAgv)
        {
            (await _dockerCompose.LoginAsync()).ShouldBeTrue();
            (await _dockerCompose.PullAsync()).ShouldBeTrue();
        }

        (await _dockerCompose.UpAsync()).ShouldBeTrue();
    }

    public async Task StopAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        (await _dockerCompose.DownAsync()).ShouldBeTrue();
        (await _dockerCompose.AreRemovedAsync()).ShouldBeTrue();
    }

    public async Task<(FakeAgv data, Guid agvCorrelationId)> AddAgv(string agvSerialNumber)
    {
        var agv = new FakeAgv(agvSerialNumber, new MqttClient(_dockerCompose.EnvFile.RabbitMqUser, _dockerCompose.EnvFile.RabbitMqPassword, _logger), _logger)
            .WithGraphTemplate(FileHelper.ReadJson(_mqttSamplePath, "Graph.json"))
            .WithAgvStatusTemplate(FileHelper.ReadJson(_mqttSamplePath, "state.json"))
            .WithOrderEventTemplate(FileHelper.ReadJson(_mqttSamplePath, "OrderEvent.json"))
            .WithAgvDataTemplate(FileHelper.ReadJson(_mqttSamplePath, "AgvData.json"))
            .WithStationListTemplate(FileHelper.ReadJson(_mqttSamplePath, "StationList.json"))
            .WithMapTemplate(FileHelper.ReadJson(_mqttSamplePath, "Map.json"));

        await agv.StartAsync();

        agv.SubscribeDataTopics();
        agv.StartSendingStatus(_cancellationTokenSource.Token);
        agv.SimulateOrderProcessing();

        var agvCorrelationId = await WaitForAgv(agv.SerialNumber);
        await SetAgvToIdleAsync(agvCorrelationId);


        return (agv, agvCorrelationId);
    }

    public async Task<Guid> WaitForAgv(string agvSerialNumber)
    {
        var agv = await _transportOrder.WaitForAgv(agvSerialNumber);
        agv.ShouldNotBeNull();

        (await _transportOrder.WaitForAgvData(agv.CorrelationId)).ShouldNotBeNull();
        var restMaps = await _transportOrder.WaitForMaps();
        var map = restMaps?.SingleOrDefault();
        map.ShouldNotBeNull();
        (await _transportOrder.WaitForGraph(map.CorrelationId)).ShouldNotBeNull();
        (await _transportOrder.WaitForMap(map.CorrelationId, agv.CorrelationId)).ShouldNotBeNull();
        return agv.CorrelationId;
    }

    public async Task WaitForAgvIdle(Guid agvCorrelationId, TimeSpan timeout)
    {
        var agv = await _transportOrder.WaitForAgvIdle(agvCorrelationId, timeout);
        agv.ShouldNotBeNull();
    }

    private async Task SetAgvToIdleAsync(Guid agvCorrelationId)
    {
        await _api.SetForceIdleAsync(agvCorrelationId);
    }

    public async Task AddAndCompleteOrder(string source, string destination, Guid agvCorrelationId)
    {
        var orderId = $"Order-{Guid.NewGuid()}";
        _logger.LogInformation("Adding Order {OrderId} from {Source} to {Destination} for agv {AgvCorrelationId}", orderId, source, destination,
            agvCorrelationId);
        var actionGroup = await _api.ActionGroupPOSTAsync(source, destination, null, null, orderId, null, null, agvCorrelationId);
        actionGroup.ShouldNotBeNull();
        actionGroup = await _transportOrder.WaitForFinishedActionGroupForOrder(actionGroup.CorrelationId);
        _logger.LogInformation("Order {OrderId} completed", orderId);
        actionGroup.ShouldNotBeNull();
    }
}