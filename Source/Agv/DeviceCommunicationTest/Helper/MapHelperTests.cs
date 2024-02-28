using System.Globalization;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

[TestClass]
public class MapHelperTests
{
    [TestMethod]
    public void GetMapDimensions_EmptyArray_ThrowsMapCreationException()
    {
        Should.Throw<MapCreationException>(() => MapHelper.GetMapDimensions(Array.Empty<byte>()));
    }

    [TestMethod]
    public void GetMapDimensions_NoMapByteValue_ThrowsMapCreationException()
    {
        Should.Throw<MapCreationException>(() => MapHelper.GetMapDimensions(Convert.FromBase64String("VGVzdGV0ZXN0ZXRlc3Rl")));
    }

    [TestMethod]
    public void GetMapDimensions_Map_ConvertsProperlyToWidthAndHeight()
    {
        var dimensions = MapHelper.GetMapDimensions(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        dimensions.Width.ShouldBe(1);
        dimensions.Height.ShouldBe(1);
    }

    [TestMethod]
    public void ParseFilename_ValidFilename_ReturnsCorrectValues()
    {
        const string filename = "example_Map_1.2_-3.4_5.6.png";

        (float scaleUpFactor, float xOffset, float yOffset) = MapHelper.ParseFilename(filename);

        scaleUpFactor.ShouldBe(1.2f);
        xOffset.ShouldBe(-3.4f);
        yOffset.ShouldBe(5.6f);
    }

    [TestMethod]
    public void ParseFilename_GermanLocale_ReturnsCorrectValues()
    {
        const string filename = "exampleMap_1.2_-3.4_5.6.png";

        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        (float scaleUpFactor, float xOffset, float yOffset) = MapHelper.ParseFilename(filename);

        scaleUpFactor.ShouldBe(1.2f);
        xOffset.ShouldBe(-3.4f);
        yOffset.ShouldBe(5.6f);
    }

    [TestMethod]
    public void ParseFilename_InvalidFilename_ThrowsFormatException()
    {
        const string filename = "invalidFilename.png";

        Should.Throw<FormatException>(() => MapHelper.ParseFilename(filename, true));
    }

    [TestMethod]
    public void ParseFilename_InvalidFilename_NoThrow_ReturnsDefaultValues()
    {
        const string filename = "invalidFilename.png";

        (float scaleUpFactor, float xOffset, float yOffset) = MapHelper.ParseFilename(filename);

        scaleUpFactor.ShouldBe(1f);
        xOffset.ShouldBe(0f);
        yOffset.ShouldBe(0f);
    }
}