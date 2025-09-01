using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class PolygonExtensionTest
{
    [TestMethod]
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

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
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
            new(5, 1, 0),
            new(2, 2, 1), new(2, 4, 2), new(5, 4, 3), new(7, 6, 4),
            new(7, 3, 5)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
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
            new(1, 4, 0), new(1, 8, 1), new(6, 9, 2), new(12, 7, 3),
            new(12, 2, 4), new(10, 6, 5), new(10, 2, 6), new(5, 2, 7),
            new(7, 5, 8), new(8, 3, 9), new(8, 6, 10), new(4, 6, 11)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
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
            new(4, 1, 0), new(2, 2, 1), new(4, 4, 2), new(2, 7, 3),
            new(4, 8, 4), new(6, 7, 5), new(4, 4, 6), new(6, 2, 7)
        };
        polygon.PointIsInPolygon(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
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
            new(1, 1, 0), new(4, 9, 1), new(10, 9, 2), new(10, 1, 3), new(6, 1, 4),
            new(5, 2, 5), new(7, 2, 6), new(7, 5, 7), new(9, 5, 8), new(9, 6, 9),
            new(7, 6, 10), new(7, 5, 11), new(4, 5, 12), new(5, 2, 13)
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