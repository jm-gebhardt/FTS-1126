using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

using MassTransit.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{

    /// <summary>
    /// Wcs Host Order Service Tests
    /// </summary>
    [TestClass]
    public class WcsHostOrderServiceTest
    {
        
        /// <summary>
        /// Invalid new order does not result in publishing a message or writing any entry
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        [TestMethod]
        [DataRow("", "", "")]
        [DataRow("Order#123", "", "")]
        public async Task NewOrder_ToAgvRowIsInValid_HandlingFailed(string orderId, string source, string destination)
        {
            var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsDbContext>();
            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(orderId, source, destination));
                await context.SaveChangesAsync();
            }

            await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());

            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                Assert.AreEqual(MessageStatus.Failed, context.ToAgv.First().MessageStatus);
                Assert.AreEqual(0, context.FromAgv.Count());
            }
        }
        
        
        /// <summary>
        /// A valid new order was found in ToAgv which results in a IHandleHostOrder message
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// </summary>
        [TestMethod]
        [DataRow( "sc1", "")]
        [DataRow( "", "dst1")]
        [DataRow( "sc1", "dst1")]
        public async Task ValidNewOrderFoundInToAgv_IHandleHostOrderWasPublished(string source, string destination)
        {
            var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsDbContext>();


            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(source: source, destination: destination));
                await context.SaveChangesAsync();
            }

 
            await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());
            
            IPublishedMessage<IHandleHostOrder>? message = fixture.TestHarness.Published.Select<IHandleHostOrder>().First();
            
            Assert.IsNotNull(message);

            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                Assert.AreEqual(1, context.FromAgv.Count());
                var toAgvEntry = context.ToAgv.First();

                Assert.AreEqual(toAgvEntry.OrderId, message.Context.Message.Order.OrderId);
                Assert.AreEqual(source, message.Context.Message.Order.Source);
                Assert.AreEqual(destination, message.Context.Message.Order.Destination);
                Assert.AreEqual(toAgvEntry.LeNo, message.Context.Message.Order.LeNo);
                Assert.AreEqual(MessageStatus.Finished, toAgvEntry.MessageStatus);
            }
        }
        
        /// <summary>
        /// A valid new order was found in ToAgv which results in a entry in FromAgv
        /// </summary>
        [TestMethod]
        [DataRow( "sc1", "")]
        [DataRow( "", "dst1")]
        [DataRow( "sc1", "dst1")]
        public async Task ValidNewOrderFoundInToAgv_FromAgvEntryCreated(string source, string destination)
        {
            var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsDbContext>();
            
            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(source: source, destination: destination));
                await context.SaveChangesAsync();
            }
            
            await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());
            
            await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
            {
                Assert.AreEqual(1, context.FromAgv.Count());
                var toAgvEntry = context.ToAgv.First();
                var fromAgvEntry = context.FromAgv.First();

                Assert.AreEqual(toAgvEntry.OrderId, fromAgvEntry.OrderId);
                Assert.AreEqual(source, fromAgvEntry.Source);
                Assert.AreEqual(destination, fromAgvEntry.Destination);
                Assert.AreEqual(MessageStatus.Pending, fromAgvEntry.MessageStatus);
                Assert.AreEqual(MessageStatus.Finished, toAgvEntry.MessageStatus);
            }
        }
    }
}