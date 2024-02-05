using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Services;

[TestClass]
public class DeserializationTest
{
    [TestMethod]
    public async Task Deserialize_ReturnsCorrectData()
    {
        string jsonContent = await File.ReadAllTextAsync("TestData/SmartLinkResponse.json");

        var result = JsonConvert.DeserializeObject<SmartLinkData>(jsonContent);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(10);
        result.ShouldContainKey("S1_Pin2");
        result["S1_Pin2"].ShouldSatisfyAllConditions(
            p => p.Direction.ShouldBe("input_output"),
            p => p.Id.ShouldBe("S1_Pin2"),
            p => p.Result.ShouldBe("ok"),
            p => p.State.ShouldBe(false));
    }
}