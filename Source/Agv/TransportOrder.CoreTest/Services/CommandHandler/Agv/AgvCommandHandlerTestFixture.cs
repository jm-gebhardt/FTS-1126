using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

public class AgvCommandHandlerTestFixture : MassTransitConsumerTestFixtureWithScheduler<AgvCommandHandler>
{
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    public readonly Mock<IAgvRepository> AgvRepository = new();
    private readonly Mock<ILogger<AgvCommandHandler>> _logger = new();
    private readonly Mock<ISettingsProvider<AgvSettings>> _settingsProvider = new();
    private readonly Mock<IAgvGraphRepository> _agvGraphRepository = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly ProductionOptions _productionOptions = new() {AutoAccept = true};
    private readonly Mock<ICargoRepository> _cargoRepository = new();
    public readonly Mock<IAgvDataRepository> AgvDataRepository = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => AgvRepository.Object);
        collection.AddTransient(_ => _logger.Object);
        collection.AddTransient(_ => _settingsProvider.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => _agvGraphRepository.Object);
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _productionOptions);
        collection.AddTransient(_ => AgvDataRepository.Object);
        collection.AddTransient(_ => _cargoRepository.Object);
    }

    public AgvCommandHandlerTestFixture(Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services) : base(busRegistrationConfigurator, inMemoryConfiguration, services)
    {
        _settingsProvider.Setup(s => s.GetSettings()).Returns(new AgvSettings
        {
            BatteryLevelCritical = 10,
            BatteryLevelGood = 80,
            BatteryLevelFull = 100,
            BatteryLevelLow = 20,
            AgvOfflineTimeoutMsWhileBusy = 10000,
            AgvOfflineTimeoutMsWhileIdle = 10000
        });
    }

    public void AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        AgvRepository.AddAgvsToMockRepository(agvs);
        AgvRepository.Setup(r => r.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()))
            .ReturnsAsync((TransportOrder.Domain.Models.Agv.Agv a) => agvs.Any(agv => agv.CorrelationId == a.CorrelationId))
            .Verifiable();
        AgvRepository.Setup(r => r.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).Verifiable();
    }

    public void SetupAgv(bool hasPendingActions = false, bool graphExists = true)
    {
        _actionGroupRepository.Setup(x =>
            x.AgvHasPendingActions(It.IsAny<Guid>())).ReturnsAsync(hasPendingActions);
        _agvGraphRepository.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(graphExists);
    }

    public void AddAction(AgvAction action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
    }

    public AgvCommandHandlerTestFixture WithSetCargoReturnValue(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos, bool returnValue)
    {
        _cargoRepository.Setup(x => x.SetCargoAsync(agvCorrelationId, cargos)).ReturnsAsync(returnValue);
        return this;
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new AgvStatusProfile(), new AgvProfile(), new CargoProfile());
        return mapper;
    }

    /// <summary>
    /// Gets the response from a request
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request">The request object</param>
    /// <returns>The response object</returns>
    public async Task<Response<TResult>> GetResponseAsync<TResult, TRequest>(TRequest request)
        where TResult : class
        where TRequest : class
    {
        IRequestClient<TRequest> client = TestHarness!.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }
}