using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters.SignificanceCheckers;

[TestClass]
public class AgvPositionSignificanceCheckerTests
{
    [TestMethod]
    public void HasSignificantChanges_WhenEqualChanges_ReturnsFalse()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { X = 1.000f, Y = 2.000f, Z = 3.000f, Theta = 45.000f, DeviationRange = 0.0005f };
        var newPosition = new AgvPositionDto { X = 1.004f, Y = 2.004f, Z = 3.004f, Theta = 45.004f, DeviationRange = 0.0006f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenPositionInitializedChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { PositionInitialized = true };
        var newPosition = new AgvPositionDto { PositionInitialized = false };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenPositionXChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { X = 1.000f };
        var newPosition = new AgvPositionDto { X = 1.006f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenPositionYChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { Y = 2.000f };
        var newPosition = new AgvPositionDto { Y = 2.005f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenPositionZChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { Z = 3.000f };
        var newPosition = new AgvPositionDto { Z = 3.007f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenThetaChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { Theta = 45.000f };
        var newPosition = new AgvPositionDto { Theta = 45.006f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenDeviationRangeChanges_ReturnsTrue()
    {
        var checker = new AgvPositionSignificanceChecker();
        var oldPosition = new AgvPositionDto { DeviationRange = 0.0005f };
        var newPosition = new AgvPositionDto { DeviationRange = 0.0016f };

        checker.HasSignificantChanges(oldPosition, newPosition).ShouldBeTrue();
    }
}