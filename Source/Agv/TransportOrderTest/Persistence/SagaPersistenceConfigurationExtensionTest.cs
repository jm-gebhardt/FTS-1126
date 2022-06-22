using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration.Saga;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Persistence
{
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
                    x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                        .AddPersistence<OrderSaga, SagaPersistenceDbContext>(true);
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
            
            var provider = collection.BuildServiceProvider(true);
            // In Memory repository adds an IndexedSagaDictionary
            Assert.IsNotNull(provider.GetService<IndexedSagaDictionary<OrderSaga>>());
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
                    
                    x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                        .AddPersistence<OrderSaga, SagaPersistenceDbContext>();
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
            
            var provider = collection.BuildServiceProvider(true);

            using var scope = provider.CreateScope();
            var sagaRepository = scope.ServiceProvider.GetService<ISagaRepositoryContextFactory<OrderSaga>>();
            Assert.IsNotNull(sagaRepository);
            Assert.AreEqual(sagaRepository.GetType().GetGenericTypeDefinition(), typeof(EntityFrameworkSagaRepositoryContextFactory<>));
        }

    }
}