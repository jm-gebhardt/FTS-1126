using System.Net;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace SmartLinkCommunicationTest.Services;

public class ModulePollingServiceTestFixture
{
    private const string SmartLinkUrl = "http://localhost/smartLink/api";
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<IServiceScope> _serviceScope = new();
    private readonly Mock<IModuleScopeFactory> _serviceScopeFactory = new();
    private readonly Mock<IRestClient> _restClient = new();
    private ModuleConfig _moduleConfig = null!;

    public Mock<ISmartLinkService> SmartLinkService { get; } = new();

    public ModulePollingService ModulePollingService { get; private set; } = null!;

    public Mock<ISmartLinkResponseHandler> SmartLinkResponseHandler { get; } = new();

    public ModulePollingServiceTestFixture WithResponse(string jsonContent)
    {
        var mockRequest = new RestRequest(new Uri(SmartLinkUrl));
        var mockResponse = new RestResponse<SmartLinkData>(mockRequest)
        {
            StatusCode = HttpStatusCode.OK,
            Content = jsonContent,
            ResponseStatus = ResponseStatus.Completed,
            IsSuccessStatusCode = true
        };
        _restClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
            .ReturnsAsync(mockResponse);
        return this;
    }

    public ModulePollingServiceTestFixture WithModuleConfig(ModuleConfig config)
    {
        _moduleConfig = config;
        return this;
    }

    public ModulePollingServiceTestFixture()
    {
        var collection = new ServiceCollection()
            .AddScoped(_ => _serviceScopeFactory.Object)
            .AddScoped(_ => _serviceScope.Object)
            .AddScoped(_ => _serviceProvider.Object);

        collection.BuildServiceProvider(true);
    }

    public ModulePollingServiceTestFixture Build()
    {
        _serviceScopeFactory.Setup(factory => factory.CreateScope(_moduleConfig))
            .Returns(_serviceScope.Object);

        _serviceScopeFactory.Setup(factory => factory.CreateScope(_moduleConfig).ServiceProvider)
            .Returns(_serviceProvider.Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(IModuleScopeFactory)))
            .Returns(_serviceScopeFactory.Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(IRestClient)))
            .Returns(_restClient.Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(IIoActionExecutionService)))
            .Returns(new Mock<IIoActionExecutionService>().Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(ILogger<SmartLinkResponseHandler>)))
            .Returns(new Mock<ILogger<SmartLinkResponseHandler>>().Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(ISmartLinkService)))
            .Returns(SmartLinkService.Object);

        _serviceProvider.Setup(provider => provider.GetService(typeof(ISmartLinkResponseHandler)))
            .Returns(SmartLinkResponseHandler.Object);

        ModulePollingService = new ModulePollingService(_serviceProvider.Object, _moduleConfig);
        return this;
    }
}