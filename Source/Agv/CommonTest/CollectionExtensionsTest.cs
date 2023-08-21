using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest;

/// <summary>
/// Test <see cref="Common.Extensions.CollectionExtensions.AddRange{T}(HashSet{T}, IEnumerable{T})"/>.
/// </summary>
[TestClass]
public class CollectionExtensionsTest
{
    /// <summary>
    /// Test xxx for <see cref="HashSet{T}"/>.
    /// </summary>
    [TestMethod]
    public void HashSet_AddRange()
    {
        var set = new HashSet<string>();
        var list = new List<string>();

        set.Add("1").ShouldBeTrue();
        set.Count.ShouldBe(1);
        list.Add("2");
        list.Add("3");
        set.AddRange(list).ShouldBeTrue();
        set.Count.ShouldBe(3);
        set.AddRange(list).ShouldBeFalse();
        set.Count.ShouldBe(3);
    }
}