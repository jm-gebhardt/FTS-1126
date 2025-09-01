using Gebhardt.Acc.Common.Extensions;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class EnumerableExtensionTest
{
    [TestMethod]
    public void IsNullOrEmpty_DictionaryStringListString_Null_ReturnsTrue()
    {
        Dictionary<string, List<string>>? dictionary = null;
        dictionary.IsNullOrEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public void IsNullOrEmpty_DictionaryStringListString_Empty_ReturnsTrue()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, List<string>> dictionary = new();
        dictionary.IsNullOrEmpty().ShouldBeTrue();
    }
}