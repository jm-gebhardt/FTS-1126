using Microsoft.VisualStudio.TestTools.UnitTesting;

using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers
{
    [TestClass]
    public class WcsOrderConsumerTest
    {
        /// <summary>
        /// Test for processing HostOrderHandlingSucceeded message
        /// </summary>
        [TestMethod]
        public async Task HostOrderSucceededMessageConsumed_OrderIdDoesNotExistInToAgv_FromAgvEntryNotWritten()
        {
            var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsDbContext>();

            string orderId = "TestOrder123";
            var correlationId = orderId.ToGuid();

            await fixture.TestHarness.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, orderId));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>());

            var message = fixture.ConsumerHarness.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(orderId, message.OrderId);

            await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
            Assert.AreEqual(0, context.FromAgv.Count());
        }

        /// <summary>
        /// Test for processing HostOrderHandlingSucceeded message
        /// </summary>
        [TestMethod]
        [DataRow(RecordType.TransportOrder)]
        [DataRow(RecordType.TransportOrderUpdate)]
        [DataRow(RecordType.OrderCancelRequest)]
        public async Task HostOrderSucceededMessageConsumed_FromAgvEntryWritten_OrderStatusFinished(RecordType recordTyp)
        {
            var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsDbContext>();

            await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
            var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
            context.ToAgv.Add(latestOrder);

            await context.SaveChangesAsync();
            var correlationId = latestOrder.OrderId.ToGuid();

            await fixture.TestHarness.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, latestOrder.OrderId));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>());

            var message = fixture.ConsumerHarness.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(latestOrder.OrderId, message.OrderId);

            // Ensure database entry created
            Assert.AreEqual(1, context.FromAgv.Count());
            var entry = context.FromAgv.First();
            Assert.AreEqual(latestOrder.OrderId, entry.OrderId);
            Assert.AreEqual(latestOrder.Source, entry.Source);
            Assert.AreEqual(latestOrder.Destination, entry.Destination);
            Assert.AreEqual(latestOrder.LeNo, entry.LeNo);
            Assert.AreEqual(RecordType.TransportOrder, entry.RecordType);
            Assert.AreEqual(OrderStatus.Finished, entry.Status);
        }

        /// <summary>
        /// Test for processing HostOrderStarted message
        /// </summary>
        [TestMethod]
        [DataRow(RecordType.TransportOrder)]
        [DataRow(RecordType.TransportOrderUpdate)]
        [DataRow(RecordType.OrderCancelRequest)]
        public async Task HostOrderStartedMessageConsumed_FromAgvEntryWritten_OrderStatusInProgress(RecordType recordTyp)
        {
            var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsDbContext>();

            await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
            var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
            context.ToAgv.Add(latestOrder);

            await context.SaveChangesAsync();
            string assignedAgv = "AGV1";
            var correlationId = latestOrder.OrderId.ToGuid();

            await fixture.TestHarness.Bus.Publish(new HostOrderStarted(correlationId, latestOrder.OrderId, assignedAgv));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderStarted>());

            var message = fixture.ConsumerHarness.Consumed.Select<HostOrderStarted>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(latestOrder.OrderId, message.OrderId);

            // Ensure database entry created
            Assert.AreEqual(1, context.FromAgv.Count());
            var entry = context.FromAgv.First();
            Assert.AreEqual(latestOrder.OrderId, entry.OrderId);
            Assert.AreEqual(latestOrder.Source, entry.Source);
            Assert.AreEqual(latestOrder.Destination, entry.Destination);
            Assert.AreEqual(latestOrder.LeNo, entry.LeNo);
            Assert.AreEqual(RecordType.TransportOrder, entry.RecordType);
            Assert.AreEqual(assignedAgv, entry.Vehicle);
        }

        [TestMethod]
        [DataRow(RecordType.TransportOrder)]
        [DataRow(RecordType.TransportOrderUpdate)]
        [DataRow(RecordType.OrderCancelRequest)]
        public async Task HostOrderHandlingFailedMessageConsumed_FromAgvEntryWritten_OrderStatusFinishedWithError(RecordType recordType)
        {
            var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsDbContext>();

            await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
            var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordType);
            context.ToAgv.Add(latestOrder);

            await context.SaveChangesAsync();

            var correlationId = latestOrder.OrderId.ToGuid();

            await fixture.TestHarness.Bus.Publish(new HostOrderHandlingFailed(correlationId, latestOrder.OrderId, HostOrderErrorCode.OrderAlreadyStarted));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>());

            var message = fixture.ConsumerHarness.Consumed.Select<HostOrderHandlingFailed>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(latestOrder.OrderId, message.OrderId);

            // Ensure database entry created
            Assert.AreEqual(1, context.FromAgv.Count());
            var entry = context.FromAgv.First();
            Assert.AreEqual(latestOrder.OrderId, entry.OrderId);
            Assert.AreEqual(latestOrder.Source, entry.Source);
            Assert.AreEqual(latestOrder.Destination, entry.Destination);
            Assert.AreEqual(latestOrder.LeNo, entry.LeNo);
            Assert.AreEqual(RecordType.TransportOrder, entry.RecordType);
            Assert.AreEqual(OrderStatus.Finished, entry.Status);
            Assert.AreEqual(HostOrderErrorCode.OrderAlreadyStarted.ToString(), entry.Error);
        }
    }
}
