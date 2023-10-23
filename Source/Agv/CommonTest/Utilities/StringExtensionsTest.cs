using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Utilities;

[TestClass]
public class StringExtensionsTest
{
    [TestMethod]
    public void ToGuid_ShouldReturnParsedGuid_WhenPassingProperGuidString()
    {
        const string guidString = "35A3AE75-3FC7-41C8-98DD-C73E3845C8F5";
        var expectedGuid = new Guid(guidString);

        var guid = guidString.ToGuid();

        expectedGuid.ShouldBe(guid);
    }

    [DataTestMethod]
    [DynamicData(nameof(InputStrings), DynamicDataSourceType.Method)]
    public void ToGuid_ShouldRemainTheSameParsedGuid_WhenParsingTheInputStringMultipleTimes(string inputString)
    {
        var guid = inputString.ToGuid();

        for (int i = 0; i < 10; i++)
        {
            inputString.ToGuid().ShouldBe(guid);
        }
    }

    private static IEnumerable<object[]> InputStrings()
    {
        yield return new object[] { "35G3AE75-3FC7-41C8-98DD-C73E3845C8F5" };
        yield return new object[] { "123456789" };
        yield return new object[] { "This is an invalid string" };
        yield return new object[] { "!@#$%^&*()" };
    }
}