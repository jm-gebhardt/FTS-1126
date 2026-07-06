using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services;

[TestClass]
public class MapHelperTest
{
    // ComputeMapGeometry constants (matching implementation)
    private const float FallbackResolution = 0.05f;
    private const int FallbackExtent = 200;

    [TestMethod]
    public void ComputeMapGeometry_EmptyNodes_ReturnsFallbackValues()
    {
        (int width, int height, float resolution, float xOffset, float yOffset) = MapHelper.ComputeMapGeometry([]);

        width.ShouldBe(FallbackExtent);
        height.ShouldBe(FallbackExtent);
        resolution.ShouldBe(FallbackResolution);
        xOffset.ShouldBe(0f);
        yOffset.ShouldBe(0f);
    }

    [TestMethod]
    public void ComputeMapGeometry_SymmetricNodes_EqualWidthAndHeight()
    {
        // Nodes at (0,0) and (10,10) → rangeX=rangeY=10 → symmetric result
        var nodes = new List<GraphNode>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 }
        };

        (int width, int height, _, _, _) = MapHelper.ComputeMapGeometry(nodes);

        width.ShouldBe(height);
    }

    [TestMethod]
    public void ComputeMapGeometry_WiderThanTall_WidthGreaterThanHeight()
    {
        // rangeX=100, rangeY=10 → totalX > totalY → width > height
        var nodes = new List<GraphNode>
        {
            new() { X = 0, Y = 0 },
            new() { X = 100, Y = 10 }
        };

        (int width, int height, _, _, _) = MapHelper.ComputeMapGeometry(nodes);

        width.ShouldBeGreaterThan(height);
    }

    [TestMethod]
    public void ComputeMapGeometry_PositiveResolution()
    {
        var nodes = new List<GraphNode>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 }
        };

        (_, _, float resolution, _, _) = MapHelper.ComputeMapGeometry(nodes);

        resolution.ShouldBeGreaterThan(0f);
    }

    [TestMethod]
    public void ComputeMapGeometry_PositiveDimensions()
    {
        var nodes = new List<GraphNode>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 }
        };

        (int width, int height, _, _, _) = MapHelper.ComputeMapGeometry(nodes);

        width.ShouldBeGreaterThan(0);
        height.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void ComputeMapGeometry_OriginAtOrigin_OffsetsEqualMargin()
    {
        // Nodes at (0,0) and (10,10) → minX=0, minY=0
        // marginX = max(2, 10*0.1) = 2, marginY = 2
        // xOffset = marginX - minX = 2 - 0 = 2, yOffset = 2
        var nodes = new List<GraphNode>
        {
            new() { X = 0, Y = 0 },
            new() { X = 10, Y = 10 }
        };

        (_, _, _, float xOffset, float yOffset) = MapHelper.ComputeMapGeometry(nodes);

        xOffset.ShouldBe(2f);
        yOffset.ShouldBe(2f);
    }

    [TestMethod]
    public void ComputeMapGeometry_SingleNode_ReturnsMinimumDimensions()
    {
        var nodes = new List<GraphNode> { new() { X = 0, Y = 0 } };

        (int width, int height, _, _, _) = MapHelper.ComputeMapGeometry(nodes);

        width.ShouldBeGreaterThan(0);
        height.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void GenerateWhitePngBase64_ReturnsNonEmptyBase64String()
    {
        string result = MapHelper.GenerateWhitePngBase64(10, 10);

        result.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void GenerateWhitePngBase64_DecodesValidPng()
    {
        string result = MapHelper.GenerateWhitePngBase64(4, 4);
        byte[] bytes = Convert.FromBase64String(result);

        // PNG signature: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
        bytes[0].ShouldBe((byte)0x89);
        bytes[1].ShouldBe((byte)0x50); // P
        bytes[2].ShouldBe((byte)0x4E); // N
        bytes[3].ShouldBe((byte)0x47); // G
    }

    [TestMethod]
    public void GenerateWhitePngBase64_LargerImage_LongerOutput()
    {
        string small = MapHelper.GenerateWhitePngBase64(4, 4);
        string large = MapHelper.GenerateWhitePngBase64(100, 100);

        large.Length.ShouldBeGreaterThan(small.Length);
    }
}
