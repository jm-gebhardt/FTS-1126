using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

[TestClass]
public class InfoToAgvMessageExtension
{
    [DataTestMethod]
    [DataRow("1", true)]
    [DataRow("2", true)]
    [DataRow("3", true)]
    [DataRow("4", false)]
    [DataRow("", false)]
    public void GetNeedsRotation_ValidData_ReturnTrueOrFalse(string rotationValue, bool needsRotation)
    {
        var infoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, rotationValue } };

        bool rotationNeeded = infoToAgv.NeedsRotation();

        rotationNeeded.ShouldBe(needsRotation);
    }

    [TestMethod]
    public void GetNeedsRotation_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        const string invalidValue = "5";

        var infoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, invalidValue } };

        Should.Throw<ArgumentOutOfRangeException>(() => infoToAgv.NeedsRotation()); 
    }

    [TestMethod]
    public void GetNeedsRotation_NoCargoRotationKey_ReturnFalse()
    {
        var infoToAgv = new Dictionary<InfoToAgvKey, string>();

        bool rotationNeeded = infoToAgv.NeedsRotation();

        rotationNeeded.ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow("1", 1d)]
    [DataRow("1.5", 1.5d)]
    [DataRow("3.35", 3.35d)]
    [DataRow("0.4", 0.4d)]
    public void GetLiftHeight_ValidData_ReturnDoubleValue(string liftHeightValue, double doubleValue)
    {
        var infoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeightValue } };

        double? liftHeightDouble = infoToAgv.GetLiftHeight();

        liftHeightDouble.ShouldBe(doubleValue);
    }
}