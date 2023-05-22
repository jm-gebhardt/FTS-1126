using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs
{
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

        [TestMethod]
        public void EmptyRegistry_NoAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();

            fixture.Registry.Count.ShouldBe(0);
            fixture.Registry.CountIdle.ShouldBe(0);
        }

        [TestMethod]
        public void OneIdleAdded_OneIdleAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();
            var agvSaga = new AgvSaga(Guid.NewGuid(), "agv0", nameof(AgvStateMachine.Idle));
            fixture.Add(agvSaga);

            fixture.Registry.Count.ShouldBe(1);
            fixture.Registry.CountIdle.ShouldBe(1);

            var readyAgv = fixture.Registry.GetIdleAgvs().First();
            readyAgv.CorrelationId.ShouldBe(agvSaga.CorrelationId);
            readyAgv.CurrentState.ShouldBe(agvSaga.CurrentState);
            readyAgv.AgvId.ShouldBe(agvSaga.AgvId);
            readyAgv.AgvStatus.ShouldBe(agvSaga.AgvStatus);
            readyAgv.AgvGraph.ShouldBe(agvSaga.AgvGraph);
            readyAgv.IsOnline.ShouldBeFalse();
        }

        [TestMethod]
        public void TwoAdded_OneIdleAGVAvailable()
        {
            var fixture = new EfAgvRegistryTestFixture();
            var agvSaga = new AgvSaga(Guid.NewGuid(), "agv0", nameof(AgvStateMachine.Idle));
            fixture.Add(agvSaga);
            fixture.Add(new AgvSaga(Guid.NewGuid(), "agv1", nameof(AgvStateMachine.Discovered)));

            fixture.Registry.Count.ShouldBe(2);
            fixture.Registry.CountIdle.ShouldBe(1);

            var readyAgv = fixture.Registry.GetIdleAgvs().First();
            readyAgv.CorrelationId.ShouldBe(agvSaga.CorrelationId);
            readyAgv.CurrentState.ShouldBe(agvSaga.CurrentState);
            readyAgv.AgvId.ShouldBe(agvSaga.AgvId);
            readyAgv.AgvStatus.ShouldBe(agvSaga.AgvStatus);
            readyAgv.AgvGraph.ShouldBe(agvSaga.AgvGraph);
        }

        [TestMethod]
        public void Verify_IAgvRegistry_Interface_Methods()
        {
            string agvId1 = "AGV001";
            string agvId2 = "AGV002";
            var correlationId1 = agvId1.ToGuid();
            var correlationId2 = agvId2.ToGuid();

            var fixture = new EfAgvRegistryTestFixture();
            fixture.Add(new AgvSaga(correlationId1, agvId1, nameof(AgvStateMachine.Idle)));
            fixture.Add(new AgvSaga(correlationId2, agvId2, nameof(AgvStateMachine.Discovered)));

            var agvRegistry = fixture.Registry;

            fixture.Registry.Count.ShouldBe(2);
            fixture.Registry.CountIdle.ShouldBe(1);

            IEnumerable<IAgv> allAgvs = agvRegistry.GetAllAgvs();
            IEnumerable<IAgv> readyAgvs = agvRegistry.GetIdleAgvs();

            // ReSharper disable PossibleMultipleEnumeration
            allAgvs.Count().ShouldBe(2);
            readyAgvs.Count().ShouldBe(1);

            foreach (var actualAgv in agvRegistry.GetAllAgvs())
            {
                var foundAgv = agvRegistry.GetAgv(actualAgv.CorrelationId);
                foundAgv.ShouldNotBeNull();
                foundAgv.AgvId.ShouldBe(actualAgv.AgvId);
            }

            agvRegistry.GetAgv(Guid.Empty).ShouldBeNull();
        }
    }
}
