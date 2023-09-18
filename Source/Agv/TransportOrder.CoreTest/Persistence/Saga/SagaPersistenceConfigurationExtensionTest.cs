using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration.Saga;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Persistence.Saga;

/// <summary>
/// SagaPersistenceConfigurationExtension Tests
/// </summary>
[TestClass]
public class SagaPersistenceConfigurationExtensionTest
{

    /// <summary>
    /// Add InMemoryRepository service added successfully
    /// </summary>
    [TestMethod]
    public void AddInMemoryRepository_ServiceAddedSuccessfully()
    {
            
        var collection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                    .AddPersistence<AgvSaga, SagaPersistenceDbContext>(true);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            });
            
        var provider = collection.BuildServiceProvider(true);
        // In Memory repository adds an IndexedSagaDictionary
        provider.GetService<IndexedSagaDictionary<AgvSaga>>().ShouldNotBeNull();
    }
        
    /// <summary>
    /// Add EntityFrameworkRepository service added successfully
    /// </summary>
    [TestMethod]
    public void AddEntityFrameworkRepository_ServiceAddedSuccessfully()
    {
            
        var collection = new ServiceCollection()
            .AddDbContextFactory<SagaPersistenceDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString())
            )
            .AddMassTransitTestHarness(x =>
            {
                    
                x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                    .AddPersistence<AgvSaga, SagaPersistenceDbContext>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            });
            
        var provider = collection.BuildServiceProvider(true);

        using var scope = provider.CreateScope();
        var sagaRepository = scope.ServiceProvider.GetService<ISagaRepositoryContextFactory<AgvSaga>>();
        sagaRepository.ShouldNotBeNull();
        typeof(EntityFrameworkSagaRepositoryContextFactory<>).ShouldBe(sagaRepository.GetType().GetGenericTypeDefinition());
    }

}