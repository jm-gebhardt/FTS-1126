using Gebhardt.Acc.Common.Domain.Utilities;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Utilities;

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

    [TestMethod]
    [DynamicData(nameof(InputStrings))]
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

    [TestMethod]
    [DataRow("InstaPick-1", "78f7b2bd-8cce-4bac-8928-af6903af6f85")]
    [DataRow("InstaPick-2", "7115cec3-30c7-6e6d-db6d-85d2ac1efdbf")]
    [DataRow("KARIS-AMR-1", "e9c4beb6-446c-9cf6-6304-d55fdd8736d6")]
    [DataRow("KARIS-Flow-1", "b8fbd408-83b9-3c87-8c91-9bc8c870f325")]
    [DataRow("Omnipallet-1", "83446ca0-130f-ed81-521a-69b3e55242ac")]
    [DataRow("Omnipallet-2", "be5fa3ef-cbf8-f815-758e-bfc0c888cd48")]
    [DataRow("KARIS0", "cab6cd15-4401-6408-c73b-d7417ccea083")]
    [DataRow("KARIS1", "8e2dec4a-6809-8516-c231-890953e42249")]
    public void ToGuid_ShouldWorkFor(string inputString, string expectedGuid)
    {
        var guid = inputString.ToGuid();

        guid.ShouldBe(new Guid(expectedGuid));
    }
}