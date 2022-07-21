using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// AGV EF Registry Test
    /// </summary>
    [TestClass]
    public class AgvEfRegistryTest
    {
        private class EfAgvRegistryTestFixture
        {
            public EfAgvRegistry Registry { get; }

            private IDbContextFactory<SagaPersistenceDbContext> ContextFactory { get; }
            private ServiceProvider Provider { get; }

            public EfAgvRegistryTestFixture()
            {
                var collection = new ServiceCollection()
                    .AddDbContextFactory<SagaPersistenceDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                        }
                    );
                Provider = collection.BuildServiceProvider(true);
                ContextFactory = Provider.GetService<IDbContextFactory<SagaPersistenceDbContext>>()!;
                Registry = new EfAgvRegistry(ContextFactory);
            }

            public void Add(AgvSaga agv)
            {
                using var context = ContextFactory.CreateDbContext();
                context.Set<AgvSaga>().Add(agv);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// EmptyRegistry_NoAGVAvailable
        /// </summary>
        [TestMethod]
        public void EmptyRegistry_NoAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();

            Assert.AreEqual(0, fixture.Registry.Count);
            Assert.AreEqual(0, fixture.Registry.CountIdle);
        }

        /// <summary>
        /// OneIdleAdded_OneIdleAGVAvailable
        /// </summary>
        [TestMethod]
        public void OneIdleAdded_OneIdleAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();
            var agvSaga = new AgvSaga(Guid.NewGuid(), "agv0", nameof(AgvStateMachine.Idle));
            fixture.Add(agvSaga);

            Assert.AreEqual(1, fixture.Registry.Count);
            Assert.AreEqual(1, fixture.Registry.CountIdle);

            var idleAgv = fixture.Registry.GetIdleAgvs().First();
            Assert.AreEqual(agvSaga.CorrelationId, idleAgv.CorrelationId);
            Assert.AreEqual(agvSaga.CurrentState, idleAgv.CurrentState);
            Assert.AreEqual(agvSaga.AgvId, idleAgv.AgvId);
            Assert.AreEqual(agvSaga.AgvStatus, idleAgv.AgvStatus);
            Assert.AreEqual(agvSaga.AgvGraph, idleAgv.AgvGraph);
            Assert.AreEqual(agvSaga.AgvData, idleAgv.AgvData);
        }

        /// <summary>
        /// OneIdleAdded_OneIdleAGVAvailable
        /// </summary>
        [TestMethod]
        public void TwoAdded_OneIdleAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();
            var agvSaga = new AgvSaga(Guid.NewGuid(), "agv0", nameof(AgvStateMachine.Idle));
            fixture.Add(agvSaga);
            fixture.Add(new AgvSaga(Guid.NewGuid(), "agv1", nameof(AgvStateMachine.Discovered)));

            Assert.AreEqual(2, fixture.Registry.Count);
            Assert.AreEqual(1, fixture.Registry.CountIdle);

            var idleAgv = fixture.Registry.GetIdleAgvs().First();
            Assert.AreEqual(agvSaga.CorrelationId, idleAgv.CorrelationId);
            Assert.AreEqual(agvSaga.CurrentState, idleAgv.CurrentState);
            Assert.AreEqual(agvSaga.AgvId, idleAgv.AgvId);
            Assert.AreEqual(agvSaga.AgvStatus, idleAgv.AgvStatus);
            Assert.AreEqual(agvSaga.AgvGraph, idleAgv.AgvGraph);
            Assert.AreEqual(agvSaga.AgvData, idleAgv.AgvData);
        }

        /// <summary>
        /// Verfy_IAgvRegistry_Interface_Methods
        /// </summary>
        [TestMethod]
        public void Verfy_IAgvRegistry_Interface_Methods()
        {
            string agvId1 = "AGV001";
            string agvId2 = "AGV002";
            var correlationId1 = agvId1.ToGuid();
            var correlationId2 = agvId2.ToGuid();

            var fixture = new EfAgvRegistryTestFixture();
            fixture.Add(new AgvSaga(correlationId1, agvId1, nameof(AgvStateMachine.Idle)));
            fixture.Add(new AgvSaga(correlationId2, agvId2, nameof(AgvStateMachine.Discovered)));

            var agvRegistry = fixture.Registry;

            Assert.AreEqual(2, agvRegistry.Count);
            Assert.AreEqual(1, agvRegistry.CountIdle);

            IEnumerable<IAgv> allAgvs = agvRegistry.GetAllAgvs();
            IEnumerable<IAgv> idleAgvs = agvRegistry.GetIdleAgvs();

            // ReSharper disable PossibleMultipleEnumeration
            Assert.AreEqual(2, allAgvs.Count());
            Assert.AreEqual(1, idleAgvs.Count());

            foreach (var actualAgv in agvRegistry.GetAllAgvs())
            {
                var foundAgv = agvRegistry.GetAgv(actualAgv.CorrelationId);
                Assert.IsNotNull(foundAgv);
                Assert.AreEqual(actualAgv.AgvId, foundAgv.AgvId);
            }

            Assert.IsNull(agvRegistry.GetAgv(Guid.Empty));
        }
    }
}
