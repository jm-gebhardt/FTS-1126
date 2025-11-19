using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Significance;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

[TestClass]
public class AgvPositionSignificanceCheckerTest
{
    private AgvPositionConfiguration _config = null!;
    private AgvPositionSignificanceChecker _checker = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new AgvPositionConfiguration(
            PositionThreshold: 0.5f,
            ThetaThreshold: 0.1f,
            DeviationRangeThreshold: 0.2f
        );
        _checker = new AgvPositionSignificanceChecker(_config);
    }

    [TestMethod]
    [DataRow(nameof(AgvPosition.X), 0.5f, true)]
    [DataRow(nameof(AgvPosition.X), -0.5f, true)]
    [DataRow(nameof(AgvPosition.X), 0.4999f, false)]
    [DataRow(nameof(AgvPosition.Y), 0.5f, true)]
    [DataRow(nameof(AgvPosition.Z), 0.5f, true)]
    [DataRow(nameof(AgvPosition.Theta), 0.1f, true)]
    [DataRow(nameof(AgvPosition.Theta), -0.1f, true)]
    [DataRow(nameof(AgvPosition.Theta), 0.0999f, false)]
    public void HasSignificantChanges_PropertyThresholdAtBoundary_ReturnsExpected(string propertyName, float difference, bool shouldDetectChange)
    {
        var basePosition = new AgvPosition(
            positionInitialized: true,
            x: 0,
            y: 0,
            z: 0,
            theta: 0,
            deviationRange: null
        );

        var modified = propertyName switch
        {
            nameof(AgvPosition.X) => basePosition with { X = difference },
            nameof(AgvPosition.Y) => basePosition with { Y = difference },
            nameof(AgvPosition.Z) => basePosition with { Z = difference },
            nameof(AgvPosition.Theta) => basePosition with { Theta = difference },
            _ => throw new ArgumentException($"Unknown property: {propertyName}")
        };

        _checker.HasSignificantChanges(modified, basePosition).ShouldBe(shouldDetectChange);
    }

    [TestMethod]
    public void HasSignificantChanges_DeviationRangeJustBelowThreshold_ReturnsFalse()
    {
        var a = new AgvPosition(true, 0, 0, 0, 0, 0.19f);
        var b = new AgvPosition(true, 0, 0, 0, 0, 0f);
        _checker.HasSignificantChanges(a, b).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_DeviationRangeAtThreshold_ReturnsTrue()
    {
        var a = new AgvPosition(true, 0, 0, 0, 0, 0.2f);
        var b = new AgvPosition(true, 0, 0, 0, 0, 0f);
        _checker.HasSignificantChanges(a, b).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_DeviationRangeAboveThreshold_ReturnsTrue()
    {
        var a = new AgvPosition(true, 0, 0, 0, 0, 0.21f);
        var b = new AgvPosition(true, 0, 0, 0, 0, 0f);
        _checker.HasSignificantChanges(a, b).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_DeviationRangeNullVsValue_ReturnsTrue()
    {
        var a = new AgvPosition(true, 0, 0, 0, 0);
        var b = new AgvPosition(true, 0, 0, 0, 0, 0f);
        _checker.HasSignificantChanges(a, b).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_PositionInitializedChanged_ReturnsTrue()
    {
        var older = new AgvPosition(false, 0, 0, 0, 0);
        var newer = new AgvPosition(true, 0, 0, 0, 0);
        _checker.HasSignificantChanges(newer, older).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_AllPropertiesIdentical_ReturnsFalse()
    {
        var p1 = new AgvPosition(true, 1, 2, 3, 4, 5);
        var p2 = new AgvPosition(true, 1, 2, 3, 4, 5);
        _checker.HasSignificantChanges(p1, p2).ShouldBeFalse();
    }
}