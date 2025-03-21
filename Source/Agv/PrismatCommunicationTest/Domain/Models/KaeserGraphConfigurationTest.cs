using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class KaeserGraphConfigurationTest
{
    [TestMethod]
    public void UpdateLiftHeights_ValidData_UpdatesTargetLiftHeight()
    {
        const string updatedKey = "2110";
        const double updatedValue = 1.444d;
        const string newKey = "new";
        const double newValue = 1.555d;
        var kaeserGraph = new KaeserGraphConfiguration();
        kaeserGraph.TargetLiftHeight["2110"].ShouldBe(2.211d, tolerance:double.Epsilon);
        kaeserGraph.TargetLiftHeight["2130"].ShouldBe(1.213d, tolerance:double.Epsilon);
        kaeserGraph.TargetLiftHeight["2210"].ShouldBe(2.221d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2230"].ShouldBe(1.223d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2310"].ShouldBe(2.231d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2330"].ShouldBe(1.233d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2410"].ShouldBe(2.241d, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight["2430"].ShouldBe(1.243d, tolerance: double.Epsilon);

        kaeserGraph.UpdateLiftHeights(new Dictionary<string, double>{{updatedKey, updatedValue}, {newKey, newValue}});
        kaeserGraph.TargetLiftHeight[newKey].ShouldBe(newValue, tolerance: double.Epsilon);
        kaeserGraph.TargetLiftHeight[updatedKey].ShouldBe(updatedValue, tolerance: double.Epsilon);
    }
}