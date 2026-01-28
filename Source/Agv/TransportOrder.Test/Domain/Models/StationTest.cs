using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class StationTest
{
    [TestMethod]
    public void UpdateAdditionalInfo_NoPrevious_TakesNewAdditionalInfo()
    {
        var station = StationGenerator.GetPickDropStation("station");
        station.AdditionalInfo.ShouldBeNull();
        var newInfo = new Dictionary<string, object?> { { "NeedsPalletReporting", true } };

        station.UpdateAdditionalInfo(newInfo);

        station.AdditionalInfo.ShouldNotBeNull();
        station.AdditionalInfo.Count.ShouldBe(newInfo.Count);
        station.AdditionalInfo.ShouldBe(newInfo);
    }

    [TestMethod]
    public void UpdateAdditionalInfo_Previous_AdditionalInfoUpdated()
    {
        const string untouchedInfo = "PickDoppelspielMode";
        const string updatedInfo = "NeedsPalletReporting";
        var station = StationGenerator.GetPickDropStation("station", additionalInfo: new Dictionary<string, object?> { { updatedInfo, true }, {untouchedInfo, "Always"} });
        station.AdditionalInfo.ShouldNotBeNull();
        station.AdditionalInfo[updatedInfo].ShouldBe(true);
        
        const string addedInfo = "NeedsCargoReporting";
        var newInfo = new Dictionary<string, object?> { { updatedInfo, false }, {addedInfo, false} };

        station.UpdateAdditionalInfo(newInfo);

        station.AdditionalInfo.ShouldNotBeNull();
        station.AdditionalInfo.Count.ShouldBe(3); // untouched + added + updated
        station.AdditionalInfo[updatedInfo].ShouldBe(false);
        station.AdditionalInfo[addedInfo].ShouldBe(false);
        station.AdditionalInfo[untouchedInfo].ShouldBe("Always");
    }
}