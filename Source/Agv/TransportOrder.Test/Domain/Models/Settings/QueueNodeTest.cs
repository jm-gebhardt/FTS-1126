using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Settings;

[TestClass]
public class QueueNodeTest
{
    [TestMethod]
    public void GetCurrentAgv_Occupied_ReturnAgvThatOccupies()
    {
        var queueNode = new QueueNode();
        var agv = Guid.NewGuid();

        queueNode.Occupy(agv);

        queueNode.GetCurrentAgv().ShouldBe(agv);
    }

    [TestMethod]
    public void GetCurrentAgv_Reserved_ReturnAgvThatReserves()
    {
        var queueNode = new QueueNode();
        var agv = Guid.NewGuid();

        queueNode.Reserve(agv);

        queueNode.GetCurrentAgv().ShouldBe(agv);
    }

    [TestMethod]
    public void GetCurrentAgv_ReserveAfterOccupy_ReturnAgvThatReserves()
    {
        var queueNode = new QueueNode();
        var agv = Guid.NewGuid();

        queueNode.Occupy(Guid.NewGuid());
        queueNode.Reserve(agv);

        queueNode.GetCurrentAgv().ShouldBe(agv);
    }

    [TestMethod]
    public void GetCurrentAgv_OccupyAfterReserve_ReturnAgvThatOccupies()
    {
        var queueNode = new QueueNode();
        var agv = Guid.NewGuid();

        queueNode.Reserve(Guid.NewGuid());
        queueNode.Occupy(agv);

        queueNode.GetCurrentAgv().ShouldBe(agv);
    }
}