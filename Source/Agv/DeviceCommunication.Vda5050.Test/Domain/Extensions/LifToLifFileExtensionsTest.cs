using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class LifToLifFileExtensionsTest
{
    [TestMethod]
    public void ToLifFile_LayoutIdsAreCollected()
    {
        var layout1 = MakeLayout();
        var layout2 = MakeLayout();
        var collection = new LifLayoutCollection([layout1, layout2], MakeMetaInfo());

        var result = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "user");

        result.LayoutIds.ShouldContain(layout1.Id);
        result.LayoutIds.ShouldContain(layout2.Id);
    }

    [TestMethod]
    public void ToLifFile_CollectionMetaInformationIsPreserved()
    {
        var metaInfo = new MetaInformation("creator-x", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), "2.0", "proj-x");
        var collection = new LifLayoutCollection([], metaInfo);

        var result = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "user");

        result.LifCollectionMetaInformation.ShouldBe(metaInfo);
    }

    [TestMethod]
    public void ToLifFile_UploadTimestampIsSet()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());
        var timestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var result = collection.ToLifFile(timestamp, "file.json", 1.0, "user");

        result.MetaInformation.UploadTimestamp.ShouldBe(timestamp);
    }

    [TestMethod]
    public void ToLifFile_FileNameIsSet()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());

        var result = collection.ToLifFile(DateTime.UtcNow, "my-layout.json", 1.0, "user");

        result.MetaInformation.Filename.ShouldBe("my-layout.json");
    }

    [TestMethod]
    public void ToLifFile_FileSizeIsSet()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());

        var result = collection.ToLifFile(DateTime.UtcNow, "file.json", 3.14, "user");

        result.MetaInformation.FileSizeMb.ShouldBe(3.14);
    }

    [TestMethod]
    public void ToLifFile_UploadedByIsSet()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());

        var result = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "john.doe");

        result.MetaInformation.UploadedBy.ShouldBe("john.doe");
    }

    [TestMethod]
    public void ToLifFile_SupportedVehicleTypes_IncludesNodeVehicleTypes()
    {
        var node = new Node("N1", new NodePosition(0, 0), [new VehicleTypeNodeProperty(Guid.NewGuid(), "TYPE_X")]);
        var layout = new Layout(Guid.NewGuid(), "L1", "1.0", [], [node], []);
        var collection = new LifLayoutCollection([layout], MakeMetaInfo());

        var result = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "user");

        result.SupportedVehicleTypes.ShouldContain("TYPE_X");
    }

    [TestMethod]
    public void ToLifFile_EachCallGeneratesDistinctId()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());

        var result1 = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "user");
        var result2 = collection.ToLifFile(DateTime.UtcNow, "file.json", 1.0, "user");

        result1.Id.ShouldNotBe(result2.Id);
    }

    private static MetaInformation MakeMetaInfo() =>
        new("creator", DateTimeOffset.UtcNow, "1.0", "proj");

    private static Layout MakeLayout() =>
        new(Guid.NewGuid(), "layout-1", "1.0", [], [], []);
}
