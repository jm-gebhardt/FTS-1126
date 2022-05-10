using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers;

/// <summary>
/// WcsOrderConsumer Tests
/// </summary>
[TestClass]
public class WcsOrderConsumer
{
    
    /// <summary>
    /// Test for processing IHostOrderHandlingSucceeded message
    /// </summary>
    [TestMethod]
    public async Task HostOrderSucceededMessageConsumed_OrderIdDoesNotExistInToAgv_FromAgvEntryNotWritten()
    {
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsDbContext>();
           
        string orderId = "TestOrder123";
        await fixture.TestHarness.Bus.Publish<IHostOrderHandlingSucceeded>(new { OrderId = orderId });

        Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderHandlingSucceeded>());
        Assert.AreEqual(orderId, fixture.ConsumerHarness.Consumed.Select<IHostOrderHandlingSucceeded>().First().Context.Message.OrderId);

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        Assert.AreEqual(0, context.FromAgv.Count());
    }
    
    /// <summary>
    /// Test for processing IHostOrderHandlingSucceeded message
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

        await fixture.TestHarness.Bus.Publish<IHostOrderHandlingSucceeded>(new {latestOrder.OrderId});
        
        Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderHandlingSucceeded>());
        
        Assert.AreEqual( latestOrder.OrderId, fixture.ConsumerHarness.Consumed.Select<IHostOrderHandlingSucceeded>().First().Context.Message.OrderId);

        // Ensure database entry created
        Assert.AreEqual(1, context.FromAgv.Count());
        var entry = context.FromAgv.First();
        Assert.AreEqual( latestOrder.OrderId, entry.OrderId);
        Assert.AreEqual( latestOrder.Source, entry.Source);
        Assert.AreEqual( latestOrder.Destination, entry.Destination);
        Assert.AreEqual( latestOrder.LeNo, entry.LeNo);
        Assert.AreEqual( RecordType.TransportOrder, entry.RecordType);
        Assert.AreEqual( OrderStatus.Finished, entry.Status);
    }
    
    /// <summary>
    /// Test for processing IHostOrderStarted message
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

        await fixture.TestHarness.Bus.Publish<IHostOrderStarted>(new {latestOrder.OrderId, assignedAgv});
        
        Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderStarted>());
        
        Assert.AreEqual( latestOrder.OrderId, fixture.ConsumerHarness.Consumed.Select<IHostOrderStarted>().First().Context.Message.OrderId);

        // Ensure database entry created
        Assert.AreEqual(1, context.FromAgv.Count());
        var entry = context.FromAgv.First();
        Assert.AreEqual( latestOrder.OrderId, entry.OrderId);
        Assert.AreEqual( latestOrder.Source, entry.Source);
        Assert.AreEqual( latestOrder.Destination, entry.Destination);
        Assert.AreEqual( latestOrder.LeNo, entry.LeNo);
        Assert.AreEqual( RecordType.TransportOrder, entry.RecordType);
        Assert.AreEqual( assignedAgv, entry.Vehicle);
    }
    
    /// <summary>
    /// Test for processing IHostOrderStarted message
    /// </summary>
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

        await fixture.TestHarness.Bus.Publish<IHostOrderHandlingFailed>(
            new {latestOrder.OrderId, Error = HostOrderErrorCode.OrderAlreadyStarted}
            );
        
        Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderHandlingFailed>());
        IReceivedMessage<IHostOrderHandlingFailed>? message = fixture.ConsumerHarness.Consumed.Select<IHostOrderHandlingFailed>().First();
        Assert.AreEqual( latestOrder.OrderId, message.Context.Message.OrderId);

        // Ensure database entry created
        Assert.AreEqual(1, context.FromAgv.Count());
        var entry = context.FromAgv.First();
        Assert.AreEqual( latestOrder.OrderId, entry.OrderId);
        Assert.AreEqual( latestOrder.Source, entry.Source);
        Assert.AreEqual( latestOrder.Destination, entry.Destination);
        Assert.AreEqual( latestOrder.LeNo, entry.LeNo);
        Assert.AreEqual( RecordType.TransportOrder, entry.RecordType);
        Assert.AreEqual( OrderStatus.Finished, entry.Status);
        Assert.AreEqual( HostOrderErrorCode.OrderAlreadyStarted.ToString(), entry.Error);
    }

}