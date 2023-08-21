using Microsoft.VisualStudio.TestTools.UnitTesting;

using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers;

[TestClass]
public class WcsOrderConsumerTest
{
    /// <summary />
    public WcsOrderConsumerTest()
    {
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();
        // Initialize EntityFramework
        fixture.DbContextFactory.CreateDbContext();
    }

    /// <summary>
    /// Test for processing HostOrderHandlingSucceeded message
    /// </summary>
    [TestMethod]
    public async Task HostOrderSucceededMessageConsumed_OrderIdDoesNotExistInToAgv_FromAgvEntryNotWritten()
    {
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        string orderId = "TestOrder123";
        var correlationId = orderId.ToGuid();

        await fixture.TestHarness!.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, orderId));

        (await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(orderId);

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.FromAgv.Count().ShouldBe(0);
    }

    /// <summary>
    /// Test for processing HostOrderHandlingSucceeded message
    /// </summary>
    /// <param name="recordTyp"></param>
    [TestMethod]
    [DataRow(RecordType.TransportOrder)]
    [DataRow(RecordType.TransportOrderUpdate)]
    [DataRow(RecordType.OrderCancelRequest)]
    public async Task HostOrderSucceededMessageConsumed_FromAgvEntryWritten_OrderStatusFinished(RecordType recordTyp)
    {
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();
        var correlationId = latestOrder.OrderId.ToGuid();

        await fixture.TestHarness!.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, latestOrder.OrderId));

        (await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(latestOrder.OrderId);

        // Ensure database entry created
        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.TransportOrder);
        entry.Status.ShouldBe(OrderStatus.Finished);
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
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();
        string assignedAgv = "AGV1";
        var correlationId = latestOrder.OrderId.ToGuid();

        await fixture.TestHarness!.Bus.Publish(new HostOrderStarted(correlationId, latestOrder.OrderId, assignedAgv));

        (await fixture.TestHarness.Published.Any<HostOrderStarted>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderStarted>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(latestOrder.OrderId);

        // Ensure database entry created
        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.TransportOrder);
        entry.Vehicle.ShouldBe(assignedAgv);
    }

    [TestMethod]
    [DataRow(RecordType.TransportOrder)]
    [DataRow(RecordType.TransportOrderUpdate)]
    [DataRow(RecordType.OrderCancelRequest)]
    public async Task HostOrderHandlingFailedMessageConsumed_FromAgvEntryWritten_OrderStatusFinishedWithError(RecordType recordType)
    {
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordType);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        var correlationId = latestOrder.OrderId.ToGuid();

        await fixture.TestHarness!.Bus.Publish(new HostOrderHandlingFailed(correlationId, latestOrder.OrderId, HostOrderHandlingFailed.ErrorCode.OrderAlreadyExists));

        (await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderHandlingFailed>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(latestOrder.OrderId);

        // Ensure database entry created
        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.TransportOrder);
        entry.Status.ShouldBe(OrderStatus.Finished);
        entry.Error.ShouldBe(HostOrderHandlingFailed.ErrorCode.OrderAlreadyExists.ToString());
    }

    [TestMethod]
    public async Task Consuming_HostOrderCancelSucceeded_Table_Entry_Examination()
    {
        // Arrange
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.OrderCancelAcknowledge);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        var correlationId = latestOrder.OrderId.ToGuid();

        // Act
        await fixture.TestHarness!.Bus.Publish(new HostOrderCancelSucceeded(correlationId, latestOrder.OrderId));

        // Assert
        (await fixture.TestHarness.Published.Any<HostOrderCancelSucceeded>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderCancelSucceeded>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(latestOrder.OrderId);

        // Table entry examination
        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.OrderCancelAcknowledge);
        entry.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [TestMethod]
    public async Task Consuming_HostOrderCancelSucceeded_But_No_Order_Found()
    {
        // Arrange
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        string orderId = "TestOrder123";
        var correlationId = orderId.ToGuid();

        // Act
        await fixture.TestHarness!.Bus.Publish(new HostOrderCancelSucceeded(correlationId, orderId));

        // Arrange
        (await fixture.TestHarness.Published.Any<HostOrderCancelSucceeded>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderCancelSucceeded>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(orderId);

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.FromAgv.Count().ShouldBe(0);
    }

    [TestMethod]
    public async Task Consuming_HostOrderCancelFailed_Table_Entry_Examination()
    {
        // Arrange
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.OrderCancelAcknowledge);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        var correlationId = latestOrder.OrderId.ToGuid();

        // Act
        await fixture.TestHarness!.Bus.Publish(new HostOrderCancelFailed(correlationId, latestOrder.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled));

        // Assert
        (await fixture.TestHarness.Published.Any<HostOrderCancelFailed>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderCancelFailed>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(latestOrder.OrderId);

        // Table entry examination
        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.OrderCancelFailed);
        entry.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [TestMethod]
    public async Task Consuming_HostOrderCancelFailed_But_No_Order_Found()
    {
        // Arrange
        var fixture = new EfConsumerTestFixture<WcsHostOrderConsumer, WcsHostDbContext>();

        string orderId = "TestOrder123";
        var correlationId = orderId.ToGuid();

        // Act
        await fixture.TestHarness!.Bus.Publish(new HostOrderCancelFailed(correlationId, orderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled));

        // Arrange
        (await fixture.TestHarness.Published.Any<HostOrderCancelFailed>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderCancelFailed>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(orderId);

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.FromAgv.Count().ShouldBe(0);
    }
}