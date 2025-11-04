using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class QueueNodeTest
{
    [TestMethod]
    public void Reserve_FreshNode_SetsReservedAgvChanged()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.Reserve(agvId);

        queueNode.ReservedAgvChanged.ShouldBeTrue();
        queueNode.ReservedAgv.ShouldBe(agvId);
        queueNode.LastReservedDate.ShouldNotBeNull();
    }

    [TestMethod]
    public void Occupy_ReservedNode_SetsOccupiedAgvChangedAndResetsReservedAgvChanged()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.Reserve(agvId);
        queueNode.Occupy(agvId);

        queueNode.OccupiedAgvChanged.ShouldBeTrue();
        queueNode.OccupiedAgv.ShouldBe(agvId);
        queueNode.LastOccupiedDate.ShouldNotBeNull();

        queueNode.ReservedAgvChanged.ShouldBeTrue();
        queueNode.ReservedAgv.ShouldBeNull();
        queueNode.LastReservedDate.ShouldBeNull();
    }

    [TestMethod]
    public void RemoveAgv_ReservedNode_ResetsReservedAgvChanged()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.Reserve(agvId);
        queueNode.ClearChangedFlags();
        queueNode.RemoveAgv(agvId);

        queueNode.ReservedAgvChanged.ShouldBeTrue();
        queueNode.ReservedAgv.ShouldBeNull();
        queueNode.LastReservedDate.ShouldBeNull();
    }

    [TestMethod]
    public void RemoveAgv_OccupiedNode_ResetsReservedAgvChanged()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.Occupy(agvId);
        queueNode.ClearChangedFlags();
        queueNode.RemoveAgv(agvId);

        queueNode.OccupiedAgvChanged.ShouldBeTrue();
        queueNode.OccupiedAgv.ShouldBeNull();
        queueNode.LastOccupiedDate.ShouldBeNull();
    }

    [TestMethod]
    public void Free_OccupiedNode_SetsReservedAgvChanged()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.Occupy(agvId);
        queueNode.Free(agvId);

        queueNode.OccupiedAgvChanged.ShouldBeTrue();
        queueNode.OccupiedAgv.ShouldBeNull();
        queueNode.LastOccupiedDate.ShouldBeNull();
    }

    [TestMethod]
    public void ClearChangedFlags_ReservedAndOccupiedNode_ResetsFlags()
    {
        var queueNode = new QueueNode { NodeId = Guid.NewGuid(), Position = 1 };
        var agvId = Guid.NewGuid();

        queueNode.ReservedAgvChanged.ShouldBeFalse();
        queueNode.OccupiedAgvChanged.ShouldBeFalse();
        queueNode.Occupy(agvId);
        queueNode.Reserve(agvId);
        queueNode.ReservedAgvChanged.ShouldBeTrue();
        queueNode.OccupiedAgvChanged.ShouldBeTrue();
        
        queueNode.ClearChangedFlags();
        
        queueNode.ReservedAgvChanged.ShouldBeFalse();
        queueNode.OccupiedAgvChanged.ShouldBeFalse();
    }
}