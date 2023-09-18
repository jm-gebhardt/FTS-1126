using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Detectors;

[TestClass]
public class ChangeDetectorTest
{
    private static AgvPositionReceived GetAgvPosition(string agvId) => new(Guid.NewGuid(), agvId, new AgvPosition(true, 0, 0, 0, 0));


    [TestMethod]
    public void HasChanged_ReturnsTrue_WhenPreviousObjectIsNull()
    {
        var changeDetector = new ChangeDetector<AgvPositionReceived>();
        changeDetector.HasChanged(null, GetAgvPosition("agv")).ShouldBeTrue();
    }

    [TestMethod]
    public void HasChanged_ReturnsTrue_WhenNewObjectIsNull()
    {
        var changeDetector = new ChangeDetector<AgvPositionReceived>();
        changeDetector.HasChanged(GetAgvPosition("agv"), null).ShouldBeTrue();
    }

    [TestMethod]
    public void HasChanged_ReturnsFalse_WhenBothObjectsAreNull()
    {
        var changeDetector = new ChangeDetector<AgvPositionReceived>();
        changeDetector.HasChanged(null, null).ShouldBeFalse();
    }

    [TestMethod]
    public void HasChanged_ReturnsTrue_WhenObjectAreNotEqual()
    {
        var changeDetector = new ChangeDetector<AgvPositionReceived>();
        changeDetector.HasChanged(GetAgvPosition("agv1"), GetAgvPosition("agv2")).ShouldBeTrue();
    }

    [TestMethod]
    public void HasChanged_ReturnsFalse_WhenObjectAreEqual()
    {
        var changeDetector = new ChangeDetector<AgvPositionReceived>();
        var agvPositionReceived = GetAgvPosition("agv1");
        changeDetector.HasChanged(agvPositionReceived, agvPositionReceived).ShouldBeFalse();
    }

}