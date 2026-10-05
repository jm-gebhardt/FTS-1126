using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;
using System.ComponentModel;

namespace Gebhardt.Acc.OrderTranslator.Test.Domain;

[TestClass]
public class StationTest
{
    [TestMethod]
    public void AsDict_AllPropertiesFound()
    {
        var station = OrderGenerator.Ps01;

        var stationDict = station.AsDict();
        foreach (var property in TypeDescriptor.GetProperties(station).Cast<PropertyDescriptor>())
        {
            stationDict.ContainsKey(property.Name).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void AsDict_Skipped_DoesNotContainSkippedProperties()
    {
        var station = OrderGenerator.Ps01;

        var stationDict = station.AsDict([nameof(Station.Name), nameof(Station.Id)]);
        stationDict.ContainsKey(nameof(Station.Name)).ShouldBeFalse();
        stationDict.ContainsKey(nameof(Station.Id)).ShouldBeFalse();
    }
}