using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    [TestClass]
    public class OrderEfRegistryTest
    {
        private static readonly string[] _orders = { "ORDER001", "ORDER002" };
        private static readonly string[] _sources = { "S1", "S2" };
        private static readonly string[] _destinations = { "D1", "D2" };
        private static readonly string[] _leNo = { "111", "222" };

        private class EfOrderRegistryTestFixture
        {
            public EfOrderRegistry Registry { get; }

            private IDbContextFactory<SagaPersistenceDbContext> ContextFactory { get; }
            private ServiceProvider Provider { get; }

            public EfOrderRegistryTestFixture()
            {
                var collection = new ServiceCollection()
                    .AddDbContextFactory<SagaPersistenceDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                        }
                    );
                Provider = collection.BuildServiceProvider(true);
                ContextFactory = Provider.GetService<IDbContextFactory<SagaPersistenceDbContext>>()!;
                Registry = new EfOrderRegistry(ContextFactory);
            }

            public void Add(OrderSaga order)
            {
                using var context = ContextFactory.CreateDbContext();
                context.Set<OrderSaga>().Add(order);
                context.SaveChanges();
            }
        }

        [TestMethod]
        public void EmptyRegistry_NoOrderAvailable()
        {
            var fixture = new EfOrderRegistryTestFixture();

            Assert.AreEqual(0, fixture.Registry.Count);
            Assert.AreEqual(0, fixture.Registry.CountUnassigned);
        }

        [TestMethod]
        public void OneStateOrderReceivedAdded_OneUnassignedOrderAvailable()
        {
            var fixture = new EfOrderRegistryTestFixture();
            var order = new OrderDetails(_orders[0].ToGuid(), _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);
            var orderSaga = new OrderSaga(_orders[0].ToGuid(), order, nameof(OrderStateMachine.OrderReceived), Guid.NewGuid());

            fixture.Add(orderSaga);

            Assert.AreEqual(1, fixture.Registry.Count);
            Assert.AreEqual(1, fixture.Registry.CountUnassigned);

            var firstOrder = fixture.Registry.GetUnassignedOrders().First();
            Assert.AreEqual(orderSaga.CorrelationId, firstOrder.CorrelationId);
            Assert.AreEqual(orderSaga.CurrentState, firstOrder.CurrentState);
            Assert.AreEqual(orderSaga.OrderDetails.Destination, firstOrder.OrderDetails.Destination);
            Assert.AreEqual(orderSaga.OrderDetails.Source, firstOrder.OrderDetails.Source);
            Assert.AreEqual(orderSaga.OrderDetails.LeNo, firstOrder.OrderDetails.LeNo);
        }

        [TestMethod]
        public void OneStateOrderReceivedAdded_OneOther_OneUnassignedOrderAvailable()
        {
            var fixture = new EfOrderRegistryTestFixture();
            var order = new OrderDetails(_orders[0].ToGuid(), _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);
            var order2 = new OrderDetails(_orders[1].ToGuid(), _orders[1], _sources[1], _destinations[1], _leNo[1], HostOrderCreator.Simulation);
            var orderSaga = new OrderSaga(_orders[0].ToGuid(), order, nameof(OrderStateMachine.OrderReceived), Guid.NewGuid());
            var orderSaga2 = new OrderSaga(_orders[1].ToGuid(), order2, nameof(OrderStateMachine.AgvRequested), Guid.NewGuid());

            fixture.Add(orderSaga);
            fixture.Add(orderSaga2);

            Assert.AreEqual(2, fixture.Registry.Count);
            Assert.AreEqual(1, fixture.Registry.CountUnassigned);

            var firstOrder = fixture.Registry.GetUnassignedOrders().First();
            Assert.AreEqual(orderSaga.CorrelationId, firstOrder.CorrelationId);
            Assert.AreEqual(orderSaga.CurrentState, firstOrder.CurrentState);
            Assert.AreEqual(orderSaga.OrderDetails.Destination, firstOrder.OrderDetails.Destination);
            Assert.AreEqual(orderSaga.OrderDetails.Source, firstOrder.OrderDetails.Source);
            Assert.AreEqual(orderSaga.OrderDetails.LeNo, firstOrder.OrderDetails.LeNo);
        }


        [TestMethod]
        public void Verify_IAOrderRegistry_Interface_Methods()
        {
            var fixture = new EfOrderRegistryTestFixture();
            var order = new OrderDetails(_orders[0].ToGuid(), _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);
            var order2 = new OrderDetails(_orders[1].ToGuid(), _orders[1], _sources[1], _destinations[1], _leNo[1], HostOrderCreator.Simulation);
            var orderSaga = new OrderSaga(_orders[0].ToGuid(), order, nameof(OrderStateMachine.OrderReceived), Guid.NewGuid());
            var orderSaga2 = new OrderSaga(_orders[1].ToGuid(), order2, nameof(OrderStateMachine.AgvRequested), Guid.NewGuid());

            fixture.Add(orderSaga);
            fixture.Add(orderSaga2);

            var orderRegistry = fixture.Registry;

            Assert.AreEqual(2, orderRegistry.Count);
            Assert.AreEqual(1, orderRegistry.CountUnassigned);

            IEnumerable<IOrder> allOrders = orderRegistry.GetAllOrders();
            IEnumerable<IOrder> unassignedOrders = orderRegistry.GetUnassignedOrders();

            // ReSharper disable PossibleMultipleEnumeration
            Assert.AreEqual(2, allOrders.Count());
            Assert.AreEqual(1, unassignedOrders.Count());

            foreach (var actualOrder in orderRegistry.GetAllOrders())
            {
                var foundOrder = orderRegistry.GetOrder(actualOrder.CorrelationId);
                Assert.IsNotNull(foundOrder);
                Assert.AreEqual(actualOrder.OrderDetails.OrderId, foundOrder.OrderDetails.OrderId);
            }

            Assert.IsNull(orderRegistry.GetOrder(Guid.Empty));
        }
    }
}
