using Gebhardt.Acc.Common.Domain.Utilities;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Utilities;

[TestClass]
public class OrientationTest
{
    private const double Delta = 1e-9;

    [TestMethod]
    [DataRow(0d, 0d, 0d)]
    [DataRow(1d, 1d, 0d)]
    [DataRow(Math.PI / 2, 0d, Math.PI / 2)]
    [DataRow(0d, Math.PI / 2, Math.PI / 2)]
    [DataRow(Math.PI, 0d, Math.PI)]
    [DataRow(2 * Math.PI, 0d, 0d)]
    [DataRow(-Math.PI / 2, Math.PI / 2, Math.PI)]
    [DataRow(3 * Math.PI / 4, -3 * Math.PI / 4, Math.PI / 2)]
    [DataRow(-3 * Math.PI / 4, 3 * Math.PI / 4, Math.PI / 2)]
    [DataRow(0.1, 2 * Math.PI - 0.1, 0.2)]
    [DataRow(5 * Math.PI, Math.PI, 0d)]
    public void GetOrientationDifference_ValidData_CorrectResult(double a, double b, double expected)
    {
        Orientation.GetOrientationDifference(a, b).ShouldBe(expected, Delta);
    }
}
