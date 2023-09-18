using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

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
        var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsHostDbContext>();
        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(orderId, source, destination));
            await context.SaveChangesAsync();
        }

        await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.First().MessageStatus.ShouldBe(MessageStatus.Failed);
            context.FromAgv.Count().ShouldBe(0);
        }
    }

    /// <summary>
    /// A valid new order was found in ToAgv which results in a HandleHostOrder message
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// </summary>
    [TestMethod]
    [DataRow("sc1", "")]
    [DataRow("", "dst1")]
    [DataRow("sc1", "dst1")]
    public async Task ValidNewOrderFoundInToAgv_HandleHostOrderWasPublished(string source, string destination)
    {
        var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsHostDbContext>();

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(source: source, destination: destination));
            await context.SaveChangesAsync();
        }

        await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());

        (await fixture.TestHarness.Published.Any<CreateHostOrder>()).ShouldBeTrue();
        IPublishedMessage<CreateHostOrder>? message = fixture.TestHarness.Published.Select<CreateHostOrder>().First();

        message.ShouldNotBeNull();

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.FromAgv.Count().ShouldBe(1);
            var toAgvEntry = context.ToAgv.First();

            message.Context.Message.OrderDetails.OrderId.ShouldBe(toAgvEntry.OrderId);
            message.Context.Message.OrderDetails.Source.ShouldBe(source);
            message.Context.Message.OrderDetails.Destination.ShouldBe(destination);
            message.Context.Message.OrderDetails.LeNo.ShouldBe(toAgvEntry.LeNo);
            toAgvEntry.MessageStatus.ShouldBe(MessageStatus.Finished);
        }
    }

    /// <summary>
    /// A valid new order was found in ToAgv which results in a entry in FromAgv
    /// </summary>
    [TestMethod]
    [DataRow("sc1", "")]
    [DataRow("", "dst1")]
    [DataRow("sc1", "dst1")]
    public async Task ValidNewOrderFoundInToAgv_FromAgvEntryCreated(string source, string destination)
    {
        var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsHostDbContext>();

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(source: source, destination: destination));
            await context.SaveChangesAsync();
        }

        await fixture.Pollable.HandleNewTransportOrders(It.IsAny<CancellationToken>());

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.FromAgv.Count().ShouldBe(1);
            var toAgvEntry = context.ToAgv.First();
            var fromAgvEntry = context.FromAgv.First();

            fromAgvEntry.OrderId.ShouldBe(toAgvEntry.OrderId);
            fromAgvEntry.Source.ShouldBe(source);
            fromAgvEntry.Destination.ShouldBe(destination);
            fromAgvEntry.MessageStatus.ShouldBe(MessageStatus.Pending);
            toAgvEntry.MessageStatus.ShouldBe(MessageStatus.Finished);
        }
    }

    [TestMethod]
    public async Task HandleIncomingCancelRequests_with_Valid_Orders()
    {
        // Arrange
        var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsHostDbContext>();

        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(source: "src01", destination: "dst01", recordType: RecordType.OrderCancelRequest, messageStatus: MessageStatus.Pending));
            await context.SaveChangesAsync();
        }

        // Act
        await fixture.Pollable.HandleIncomingCancelRequests(It.IsAny<CancellationToken>());

        // Assert
        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.FromAgv.Count().ShouldBe(1);
            var toAgvEntry = context.ToAgv.First();
            var fromAgvEntry = context.FromAgv.First();

            fromAgvEntry.OrderId.ShouldBe(toAgvEntry.OrderId);
            fromAgvEntry.Source.ShouldBe("src01");
            fromAgvEntry.Destination.ShouldBe("dst01");
            fromAgvEntry.MessageStatus.ShouldBe(MessageStatus.Pending);
            toAgvEntry.MessageStatus.ShouldBe(MessageStatus.Finished);
        }
    }

    [TestMethod]
    [DataRow("", "", "")]
    [DataRow("Order123", "", "")]
    public async Task HandleIncomingCancelRequests_with_Invalid_Orders(string orderId, string source, string destination)
    {
        // Arrange
        var fixture = new EfPollableServiceTestFixture<WcsOrderHandler, WcsHostDbContext>();
        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(orderId, source, destination, recordType: RecordType.OrderCancelRequest, messageStatus: MessageStatus.Pending));
            await context.SaveChangesAsync();
        }

        // Act
        await fixture.Pollable.HandleIncomingCancelRequests(It.IsAny<CancellationToken>());

        // Assert
        await using (var context = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            context.ToAgv.First().MessageStatus.ShouldBe(MessageStatus.Failed);
            context.FromAgv.Count().ShouldBe(0);
        }
    }
}