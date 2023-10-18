using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface.Models;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.WcsApi;

[TestClass]
public class WcsCommunicatorTest
{
    [TestMethod]
    [DataRow(RecordType.TransportOrder)]
    [DataRow(RecordType.TransportOrderUpdate)]
    [DataRow(RecordType.OrderCancelRequest)]
    public async Task TransmitHostOrderHandlingSucceeded_FromAgvEntryWritten_OrderStatusFinished(RecordType recordTyp)
    {
        var fixture = new WcsCommunicatorTestFixture();
        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        (await fixture.Communicator.TransmitHostOrderHandlingSucceeded(latestOrder.OrderId)).ShouldBeTrue();

        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.TransportOrder);
        entry.Status.ShouldBe(OrderStatus.Finished);
    }

    [TestMethod]
    public async Task TransmitHostOrderHandlingSucceeded_OrderIdDoesNotExistInToAgv_ThrowsOrderNotFoundExceptionFromAgvEntryNotWritten()
    {
        var fixture = new WcsCommunicatorTestFixture();
        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();

        string orderId = "TestOrder123";
        Should.Throw<OrderNotFoundException>(async () => await fixture.Communicator.TransmitHostOrderHandlingSucceeded(orderId));

        context.FromAgv.Count().ShouldBe(0);
    }

    [TestMethod]
    [DataRow(RecordType.TransportOrder)]
    [DataRow(RecordType.TransportOrderUpdate)]
    [DataRow(RecordType.OrderCancelRequest)]
    public async Task TransmitHostOrderStarted_FromAgvEntryWritten_OrderStatusInProgress(RecordType recordTyp)
    {
        var fixture = new WcsCommunicatorTestFixture();
        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordTyp);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();
        string assignedAgv = "AGV1";

        (await fixture.Communicator.TransmitHostOrderStarted(latestOrder.OrderId, assignedAgv)).ShouldBeTrue();

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
    public async Task TransmitHostOrderHandlingFailure_FromAgvEntryWritten_OrderStatusFinishedWithError(RecordType recordType)
    {
        var fixture = new WcsCommunicatorTestFixture();
        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: recordType);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();
        string error = HostOrderHandlingFailedErrorCode.OrderAlreadyExists.ToString();

        (await fixture.Communicator.TransmitHostOrderHandlingFailure(latestOrder.OrderId, error)).ShouldBeTrue();

        context.FromAgv.Count().ShouldBe(1);
        var entry = context.FromAgv.First();
        entry.OrderId.ShouldBe(latestOrder.OrderId);
        entry.Source.ShouldBe(latestOrder.Source);
        entry.Destination.ShouldBe(latestOrder.Destination);
        entry.LeNo.ShouldBe(latestOrder.LeNo);
        entry.RecordType.ShouldBe(RecordType.TransportOrder);
        entry.Status.ShouldBe(OrderStatus.Finished);
        entry.Error.ShouldBe(error);
    }

    [TestMethod]
    public async Task TransmitHostOrderCancellationSucceeded_ValidData_EntryCreated()
    {
        var fixture = new WcsCommunicatorTestFixture();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.OrderCancelAcknowledge);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        (await fixture.Communicator.TransmitHostOrderCancellationSucceeded(latestOrder.OrderId)).ShouldBeTrue();

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
    public async Task TransmitHostOrderCancellationSucceeded_OrderIdDoesNotExistInToAgv_ThrowsOrderNotFoundExceptionFromAgvEntryNotWritten()
    {
        var fixture = new WcsCommunicatorTestFixture();
        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();

        string orderId = "TestOrder123";

        Should.Throw<OrderNotFoundException>(async() => await fixture.Communicator.TransmitHostOrderCancellationSucceeded(orderId));

        context.FromAgv.Count().ShouldBe(0);
    }

    [TestMethod]
    public async Task TransmitHostOrderCancellationFailure_ValidData_EntryCreated()
    {
        var fixture = new WcsCommunicatorTestFixture();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry());
        context.ToAgv.Add(ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.TransportOrderUpdate));
        var latestOrder = ToFromAgvGenerator.GenerateToAgvEntry(recordType: RecordType.OrderCancelAcknowledge);
        context.ToAgv.Add(latestOrder);

        await context.SaveChangesAsync();

        (await fixture.Communicator.TransmitHostOrderCancellationFailure(latestOrder.OrderId, HostOrderCancelFailedErrorCode.OrderAlreadyCancelled.ToString())).ShouldBeTrue();

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
    public async Task TransmitHostOrderCancellationFailure_OrderIdDoesNotExistInToAgv_ThrowsOrderNotFoundExceptionFromAgvEntryNotWritten()
    {
        var fixture = new WcsCommunicatorTestFixture();

        await using var context = await fixture.DbContextFactory.CreateDbContextAsync();

        Should.Throw<OrderNotFoundException>(async() => await fixture.Communicator.TransmitHostOrderCancellationFailure("unknownOrder", HostOrderCancelFailedErrorCode.OrderAlreadyCancelled.ToString()));

        context.FromAgv.Count().ShouldBe(0);
    }
}