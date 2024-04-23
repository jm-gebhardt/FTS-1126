using Gebhardt.Acc.SmartLinkCommunication.Models;
using Gebhardt.Acc.SmartLinkCommunication.Services;
using Gebhardt.Acc.SmartLinkCommunicationTest.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.SmartLinkCommunicationTest.Services;

public class SmartLinkResponseHandlerTestFixture
{
    private ModuleConfig? _moduleConfig;
    private readonly Mock<ILogger<SmartLinkResponseHandler>> _logger = new();

    private Mock<IModuleConfigProvider> ModuleConfigProvider { get; } = new();

    private Mock<IIoActionExecutionService> IoActionExecutionService { get; } = new();

    private Mock<IIoStateTrackerService> IoStateTrackerService { get; } = new();

    public SmartLinkResponseHandler SmartLinkResponseHandler { get; private set; }

    public SmartLinkResponseHandlerTestFixture()
    {
        SmartLinkResponseHandler = new SmartLinkResponseHandler(IoActionExecutionService.Object, ModuleConfigProvider.Object, IoStateTrackerService.Object, _logger.Object);
    }

    public SmartLinkResponseHandlerTestFixture WithModuleConfig(ModuleConfig moduleConfig)
    {
        _moduleConfig = moduleConfig;
        return this;
    }

    public async Task<SmartLinkResponseHandlerTestFixture> BuildAsync()
    {
        _moduleConfig ??= (await ConfigGenerator.GenerateConfigAsync()).Modules.First();
        ModuleConfigProvider.Setup(x => x.ModuleConfig).Returns(_moduleConfig);
        return this;
    }
}