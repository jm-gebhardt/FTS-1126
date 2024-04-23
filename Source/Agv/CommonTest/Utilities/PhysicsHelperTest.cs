using Gebhardt.Acc.Common.Domain.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Utilities;

[TestClass]
public class PhysicsHelperTest
{
    [TestMethod]
    [DataRow(65d, 3d, 704d + 1 / 6d)]
    [DataRow(0d, 100d, 0d)]
    [DataRow(100d, 25d, 200d)]
    public void DistanceToVelocity_ValidInput_CorrectResult(double maxVelocity, double acceleration, double expectedDistance)
    {
        PhysicsHelper.DistanceToVelocity(acceleration, maxVelocity).ShouldBe(expectedDistance, 0.0001);
    }

    [TestMethod]
    [DataRow(100d, 25d, 0d, 200d)]
    [DataRow(100d, 25d, 10d, 198d)]
    [DataRow(10d, 25d, 10d, 0d)]
    public void DistanceToVelocityFromVelocity_ValidInput_CorrectResult(double maxVelocity, double acceleration, double initialVelocity, double expectedDistance)
    {
        PhysicsHelper.DistanceToVelocityFromVelocity(acceleration, maxVelocity, initialVelocity).ShouldBe(expectedDistance, 0.0001);
    }

    [TestMethod]
    [DataRow(25d, 10d, 0.894427d)]
    [DataRow(10d, 100d, 4.472135d)]
    [DataRow(10d, 0d, 0d)]
    public void TimeToAccelerateFromStanding_ValidInput_CorrectResult(double acceleration, double distance, double expectedTime)
    {
        PhysicsHelper.TimeToAccelerateFromStanding(acceleration, distance).ShouldBe(expectedTime, 0.0001);
    }

    [TestMethod]
    [DataRow(10d, 100d, 0d, 4.472135d)]
    [DataRow(25d, 10d, 0d, 0.894427d)]
    [DataRow(10d, 100d, 1d, 4.373253d)]
    [DataRow(25d, 10d, 12d, 0.535086d)]
    [DataRow(10d, 0d, 100d, 0d)]
    [DataRow(1d, 10d, 3d, 2.38516d)]
    [DataRow(1d, 10d, 2d, 2.89897d)]
    [DataRow(1d, 10d, 1d, 3.58257d)]
    [DataRow(1d, 10d, 0d, 4.47213d)]
    public void TimeToAccelerateFromVelocity_ValidInput_CorrectResult(double acceleration, double distance, double velocity, double time)
    {
        PhysicsHelper.TimeToAccelerateFromVelocity(acceleration, distance, velocity).ShouldBe(time, 0.0001);
    }

    [TestMethod]
    [DataRow(0d, 2d, 10d, 10d)]
    [DataRow(50d, 99d, 10d, 100d)]
    [DataRow(75d, 2d, 10d, 20d)]
    public void VelocityAfterDistanceFromVelocity_ValidInput_CorrectResult(double acceleration, double distance, double initialVelocity, double finalVelocity)
    {
        PhysicsHelper.VelocityAfterDistanceFromVelocity(acceleration, distance, initialVelocity).ShouldBe(finalVelocity, 0.0001);
    }

    [TestMethod]
    [DataRow(1d, 0d, 1d, 1d, 0d)]
    [DataRow(1d, 1d, 1d, 1d, 1d)]
    [DataRow(1d, 10d, 2d, 100d, 5.0024d)]
    [DataRow(0d, 10d, 2d, 100d, 5.01d)]
    public void CalculateTimeToAccelerateAndMoveAtMaxVelocity_CanReachMaxVelocity_ReachesMaxVelocity(double initialVelocity, double distance, double maxVelocity, double acceleration, double expectedTime)
    {
        double result = PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(acceleration, distance, maxVelocity, initialVelocity, out double finalVelocity);
        finalVelocity.ShouldBe(maxVelocity, 0.0001);
        result.ShouldBe(expectedTime, 0.0001);
    }

    [TestMethod]
    [DataRow(3d, 10d, 20d, 3d, 1.76887d, 8.30662d)]
    [DataRow(3d, 10d, 10d, 1d, 2.38516d, 5.38516d)]
    [DataRow(2d, 10d, 10d, 1d, 2.89897d, 4.89897d)]
    [DataRow(1d, 10d, 10d, 1d, 3.58257d, 4.58257d)]
    [DataRow(0d, 10d, 10d, 1d, 4.47213d, 4.47213d)]
    public void CalculateTimeToAccelerateAndMoveAtMaxVelocity_CanNotReachMaxVelocity_DoesNotReachMaxVelocity(double initialVelocity, double distance, double maxVelocity, double acceleration, double expectedTime, double expectedFinalVelocity)
    {
        double result = PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(acceleration, distance, maxVelocity, initialVelocity, out double finalVelocityResult);
        finalVelocityResult.ShouldBe(expectedFinalVelocity, 0.0001);
        result.ShouldBe(expectedTime, 0.0001);
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_ValidData_CorrectResult()
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(10, 100, 10, 0, out double accelerate, out double maxVel, out double deceleration);
        accelerate.ShouldBe(5, 0.0001);
        deceleration.ShouldBe(accelerate, 0.0001);
        maxVel.ShouldBe(100 - deceleration - accelerate, 0.0001);
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_NotEnoughForMaxVelocity_MaxVelocitySectionIsZero()
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(10, 5, 10, 0, out double accelerate, out double maxVel, out double deceleration);
        accelerate.ShouldBe(2.5, 0.0001);
        deceleration.ShouldBe(accelerate, 0.0001);
        maxVel.ShouldBe(0, 0.0001);
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_NotEnoughForMaxVelocityWithInitialVelocity_MaxVelocitySectionIsZero_AccelerateIsShorterThanDecelerate()
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(10, 5, 10, 5, out double accelerate, out double maxVel, out double deceleration);
        accelerate.ShouldBe(1.875, 0.0001);
        deceleration.ShouldBe(5 - accelerate, 0.0001);
        maxVel.ShouldBe(0, 0.0001);
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_NotEnoughSpaceToDecelerate_DecelerateSectionLongerThanTotalDistance()
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(10, 4, 10, 9, out double accelerate, out double maxVel, out double deceleration);
        accelerate.ShouldBe(0, 0.0001);
        deceleration.ShouldBe(5, 0.0001);
        maxVel.ShouldBe(0, 0.0001);
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_ExactSpaceToDecelerateFromInitialVelocity_DecelerateSectionEqualToDistance()
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(10, 5, 11, 10, out double accelerate, out double maxVel, out double deceleration);
        accelerate.ShouldBe(0, 0.0001);
        deceleration.ShouldBe(5, 0.0001);
        maxVel.ShouldBe(0, 0.0001);
    }

    [TestMethod]
    public void DistanceToVelocity_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.DistanceToVelocity(0, 1));
    }

    [TestMethod]
    public void DistanceToVelocityFromVelocity_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.DistanceToVelocityFromVelocity(0, 1, 1));
    }

    [TestMethod]
    public void TimeToAccelerateFromStanding_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromStanding(0, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromStanding(-1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromStanding(1, -1));
    }

    [TestMethod]
    public void TimeToAccelerateFromVelocity_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromVelocity(0, 1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromVelocity(-1, 1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.TimeToAccelerateFromVelocity(1, -1, 1));
    }

    [TestMethod]
    public void VelocityAfterDistanceFromVelocity_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.VelocityAfterDistanceFromVelocity(1, -1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.VelocityAfterDistanceFromVelocity(-1, 1, 1));
    }

    [TestMethod]
    public void CalculateTimeToAccelerateAndMoveAtMaxVelocity_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(1, 1, 0, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(0, 1, 1, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(-1, 1, 1, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(1, -1, 1, 0));
    }

    [TestMethod]
    public void CalculateAccelerateMaxVelocityDecelerateSections_InvalidData_ThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(0, 1, 1, 0, out _, out _, out _));
    }
}