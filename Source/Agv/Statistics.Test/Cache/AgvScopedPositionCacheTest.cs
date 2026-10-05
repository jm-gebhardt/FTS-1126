using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Cache;

[TestClass]
public class AgvScopedPositionCacheTest
{
    private const string Serial = "AGV-123";
    private static readonly AgvPosition _position = AgvPositionGenerator.Generate();

    [TestMethod]
    public void ThenTryGet_AddedItem_ReturnsItem()
    {
        var timeStamp = DateTime.UtcNow;
        var expectedId = Guid.NewGuid();

        var fixture = new AgvScopedPositionCacheFixture();
        fixture.Cache.AddItem(Serial, _position, timeStamp, expectedId);
        bool found = fixture.Cache.TryGet(Serial, _position, timeStamp, out var actualList);

        found.ShouldBeTrue();
        actualList.ShouldNotBeNull();
        actualList.Count.ShouldBe(1);
        actualList[0].ShouldBe(expectedId);
    }

    [TestMethod]
    public void TryGet_SamePositionWithDifferentTimeStamp_ReturnsOnlyOne()
    {
        var timeStamp1 = DateTime.UtcNow;
        var timeStamp2 = timeStamp1.AddSeconds(1);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var fixture = new AgvScopedPositionCacheFixture();
        fixture.Cache.AddItem(Serial, _position, timeStamp1, id1);
        fixture.Cache.AddItem(Serial, _position, timeStamp2, id2);

        bool found1 = fixture.Cache.TryGet(Serial, _position, timeStamp1, out var list1);
        found1.ShouldBeTrue();
        list1!.Count.ShouldBe(1);
        list1[0].ShouldBe(id1);

        bool found2 = fixture.Cache.TryGet(Serial, _position, timeStamp2, out var list2);
        found2.ShouldBeTrue();
        list2!.Count.ShouldBe(1);
        list2[0].ShouldBe(id2);

        var missingTs = timeStamp2.AddSeconds(1);
        bool found3 = fixture.Cache.TryGet(Serial, _position, missingTs, out _);
        found3.ShouldBeFalse();
    }
}