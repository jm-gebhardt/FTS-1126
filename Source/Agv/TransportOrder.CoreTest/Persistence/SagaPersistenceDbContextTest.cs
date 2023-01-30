using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Persistence
{
    /// <summary>
    /// SagaPersistence DbContextTest
    /// </summary>
    [TestClass]
    public class SagaPersistenceDbContextTest
    {
        private class DbSetSagaPersistenceDbContext : SagaPersistenceDbContext
        {
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            public DbSet<OrderSaga> OrderSagas { get; set; }  = null!;
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            public DbSet<AgvSaga> AgvSagas { get; set; }  = null!;

            public DbSetSagaPersistenceDbContext(): this(new DbContextOptions<DbSetSagaPersistenceDbContext>())
            {
            
            }
        
            public DbSetSagaPersistenceDbContext(DbContextOptions<DbSetSagaPersistenceDbContext> options) : base(options)
            {
            
            }
        }
 
        /// <summary>
        /// Test if all OrderSaga Properties are restored
        /// </summary>
        [TestMethod]
        public void DbContext_SaveOrderSaga_AllPropertiesRestored()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<DbSetSagaPersistenceDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    }
                );
            var provider = collection.BuildServiceProvider(true);
            var dbContextFactory = provider.GetService<IDbContextFactory<DbSetSagaPersistenceDbContext>>()!;
        
            string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var order = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
            var orderSaga = new OrderSaga(correlationId, order)
            {
                CurrentState = "TestState"
            };
            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                dbContext.Database.EnsureCreated();
            
                dbContext.OrderSagas.Add(orderSaga);
                dbContext.SaveChanges();
            }

            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                var restoredOrderSaga = dbContext.OrderSagas.First();
                // Test if restore objects contains all properties by comparing JSON converted
                JsonConvert.SerializeObject(restoredOrderSaga).ShouldBe(JsonConvert.SerializeObject(orderSaga));
            }
        }
    
        /// <summary>
        /// Test if all AgvState properties are restored
        /// </summary>
        [TestMethod]
        public void DbContext_SaveAgvState_AllPropertiesRestored()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<DbSetSagaPersistenceDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    }
                );
            var provider = collection.BuildServiceProvider(true);
            var dbContextFactory = provider.GetService<IDbContextFactory<DbSetSagaPersistenceDbContext>>()!;
        
            const string agvId = "AGV1";
            var correlationId = agvId.ToGuid();
            const string mapName = "Map#1";
            var agvSaga = new AgvSaga(correlationId, agvId, "TestState", 
                new AgvStatus(Guid.NewGuid(), "123", true, true, 10,false, false),
                new AgvGraph(Guid.NewGuid(), 
                    new AgvGraph.Node[] { new("id") }, 
                    new AgvGraph.Edge[] { new("from", "to") },
                    mapName.ToGuid(),
                    mapName)
            );
            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                dbContext.Database.EnsureCreated();
            
                dbContext.AgvSagas.Add(agvSaga);
                dbContext.SaveChanges();
            }

            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                var restoredAgvSaga = dbContext.AgvSagas.First();
                // Test if restore objects contains all properties by comparing JSON converted
                JsonConvert.SerializeObject(restoredAgvSaga).ShouldBe(JsonConvert.SerializeObject(agvSaga));
            }
        }
    
    }
}