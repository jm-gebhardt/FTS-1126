using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class PolygonExtensionTest
{
    [DataTestMethod]
    [DataRow(2, 2, true)]
    [DataRow(4, 2, false)]
    public void PointIsInPolygon_SimplePolygon_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched polygon:

            1 2 3 4
            3 *---*
            2 | x | x
            1 *---*

        Key:
        - "*" represents a polygon corner.
        - "x" represents the location of the point
        */

        var polygon = new List<PolygonPoint> { new(1, 1), new(1, 3), new(3, 3), new(3, 1) };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [DataTestMethod]
    [DataRow(6, 4, true)]
    [DataRow(8, 4, false)]
    public void PointIsInPolygon_OnThreshold_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched polygon (straight edges drawn to aid understanding the shape):

               1 2 3 4 5 6 7
             6             *
             5             |
             4   *-----* x | x
             3   |         *
             2   *
             1        *


        Key:
        - "*" represents a polygon corner.
        - "x" represents the location of the point
        */

        var polygon = new List<PolygonPoint>
        {
            new(5, 1),
            new(2, 2), new(2, 4), new(5, 4), new(7, 6),
            new(7, 3)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [DataTestMethod]
    [DataRow(9, 5, true)]
    [DataRow(6, 5, false)]
    public void PointIsInPolygon_Concave_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched polygon (straight edges drawn to aid understanding the shape):

            1 2 3 4 5 6 7 8 9 10 11 12
          9           *
          8 *
          7 |                       *
          6 |     *-------*         |
          5 |     | x   * | x  *    |
          4 *     |       |    |    |
          3   *---*       *    |    |
          2         *--------- *    *
          1


         Key:
         - "*" represents a polygon corner.
         - "x" represents the location of the point
         */
        var polygon = new List<PolygonPoint>
        {
            new(1, 4), new(1, 8), new(6, 9), new(12, 7),
            new(12, 2), new(10, 6), new(10, 2), new(5, 2),
            new(7, 5), new(8, 3), new(8, 6), new(4, 6)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [DataTestMethod]
    [DataRow(4, 6, true)]
    [DataRow(5, 4, false)]
    public void PointIsInPolygon_CrossingLines_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched polygon:

            1 2 3 4 5 6
          8       *
          7   *       *
          6       x
          5
          4       * x
          3
          2   *       *
          1       *


         Key:
         - "*" represents a polygon corner.
         - "x" represents the location of the point
         */
        var polygon = new List<PolygonPoint>
        {
            new(4, 1), new(2, 2), new(4, 4), new(2, 7),
            new(4, 8), new(6, 7), new(4, 4), new(6, 2)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [DataTestMethod]
    [DataRow(6, 6, true)]
    [DataRow(6, 4, false)]
    public void PointIsInPolygon_Clockwise_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched polygon (straight edges drawn to aid understanding the shape)::

            1 2 3 4 5 6 7 8 9 10
          9       *-----------*
          8                   |
          7                   |
          6           x *---* |
          5       *-----*---* |
          4           x |     |
          3             |     |
          2         *---*     |
          1 *         *-------*


         Key:
         - "*" represents a polygon corner.
         - "x" represents the location of the point
         */
        var polygon = new List<PolygonPoint>
        {
            new(1, 1), new(4, 9), new(10, 9), new(10, 1), new(6, 1),
            new(5, 2), new(7, 2), new(7, 5), new(9, 5), new(9, 6),
            new(7, 6), new(7, 5), new(4, 5), new(5, 2)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
    public void PointIsInPolygon_EmptyPolygon_ReturnsFalse()
    {
        var polygon = new List<PolygonPoint>();
        polygon.PointIsInPolygon(12, 12).ShouldBeFalse();
    }
}