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
}