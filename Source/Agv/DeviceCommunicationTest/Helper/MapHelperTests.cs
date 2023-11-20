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
}