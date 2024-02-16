using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Configurations;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

public class AgvCommandHandlerTestFixture(
    Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
    Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
    params Action<IServiceCollection>[] services)
    : MassTransitConsumerTestFixtureWithScheduler<AgvCommandHandler>(busRegistrationConfigurator, inMemoryConfiguration, services)
{
    private readonly SemaphoreProvider<IAgvsSemaphore> _semaphoreProvider = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<ILogger<AgvCommandHandler>> _logger = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly ProductionOptions _productionOptions = new() { AutoAccept = true };
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    public readonly Mock<IAgvDb> AgvDb = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient<ISemaphoreProvider<IAgvsSemaphore>>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _logger.Object);
        collection.AddFakeSettings(new AgvSettings());
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _productionOptions);
        collection.AddTransient(_ => AgvDb.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
    }

    public AgvCommandHandlerTestFixture AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        AgvDb.AddAgvsToMockRepository(agvs);
        AgvDb.Setup(r => r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()))
            .ReturnsAsync((TransportOrder.Domain.Models.Agv.Agv a) => agvs.Any(agv => agv.CorrelationId == a.CorrelationId))
            .Verifiable();
        AgvDb.Setup(r => r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).Verifiable();

        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture SetupAgv(bool hasPendingActions = false, bool graphExists = true)
    {
        _actionGroupRepository.Setup(x =>
            x.AgvHasPendingActions(It.IsAny<Guid>())).ReturnsAsync(hasPendingActions);
        AgvDb.Setup(x => x.Graphs.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(graphExists);

        return this;
    }

    [PublicAPI]
    public AgvCommandHandlerTestFixture WithCurrentAgvAction(AgvAction action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
        return this;
    }

    public AgvCommandHandlerTestFixture WithHasStartedAction(bool hasStartedAction)
    {
        _actionGroupRepository.Setup(x => x.HasStartedActionAsync(It.IsAny<Guid>())).ReturnsAsync(hasStartedAction);
        return this;
    }

    public AgvCommandHandlerTestFixture WithSetCargoReturnValue(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos, bool returnValue)
    {
        AgvDb.Setup(x => x.Cargos.SetCargoAsync(agvCorrelationId, cargos)).ReturnsAsync(returnValue);
        return this;
    }

    public AgvCommandHandlerTestFixture WithHasCargoStates(params bool[] hasCargos)
    {
        ISetupSequentialResult<Task<bool>> setup = AgvDb.SetupSequence(x => x.Cargos.HasAgvCargoAsync(It.IsAny<Guid>()));
        foreach (bool hasCargo in hasCargos)
        {
            setup.ReturnsAsync(hasCargo);
        }
        return this;
    }

    public AgvCommandHandlerTestFixture WithGraphExists(bool exists)
    {
        AgvDb.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(exists);
        return this;
    }

    public AgvCommandHandlerTestFixture WithNodeExists(bool exists)
    {
        AgvDb.Setup(x => x.Graphs.NodeExistsAsync(It.IsAny<string>())).ReturnsAsync(exists);
        return this;
    }

    public AgvCommandHandlerTestFixture WithoutAgv()
    {
        AgvDb.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => throw new AgvNotFoundException(id));
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
        IRequestClient<TRequest> client = TestHarness.Bus!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);
        return response;
    }
}