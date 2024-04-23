using Gebhardt.Acc.Common.Domain.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Models;

[TestClass]
public class ReasonTest
{
    [TestMethod]
    public void Reason_WithValidInput_ShouldInitializeCorrectly()
    {
        var reason = new Reason("Error occurred at line {0}", "42");

        reason.ToString().ShouldBe("Error occurred at line 42");
    }

    [TestMethod]
    public void Reason_WithNoPlaceholders_ShouldReturnTemplate()
    {
        var reason = new Reason("Operation completed successfully");

        reason.ToString().ShouldBe("Operation completed successfully");
    }

    [TestMethod]
    public void Reason_WithExcessPlaceholders_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("Error {0} occurred at {1}", "404"));
    }

    [TestMethod]
    public void Reason_WithExcessParameters_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("Error {0}", "404", "403"));
    }

    [TestMethod]
    public void Reason_WithParameters_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("Error", "404"));
    }

    [TestMethod]
    public void Reason_WithValidInput_ReturnsFilledOutTemplate()
    {
        var reason = new Reason("Error {0} occurred at line {1}", "404", "42");
        reason.ToString().ShouldBe("Error 404 occurred at line 42");
    }

    [TestMethod]
    public void Reason_MultipleSamePlaceholders_ShouldReplaceAllOccurrences()
    {
        var reason = new Reason("{0} occurred at {0}", "Error");

        reason.ToString().ShouldBe("Error occurred at Error");
    }

    [TestMethod]
    public void Reason_WithEmptyTemplate_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason(string.Empty, "Some Value"));
    }

    [TestMethod]
    public void Reason_WithNullParameters_ShouldFillOutWithNull()
    {
        var reason = new Reason("Error {0} occurred", (string?)null);

        reason.ToString().ShouldBe("Error  occurred");
    }

    [TestMethod]
    public void Reason_PlaceholdersOutOfRange_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("Value: {2}", "10", "20"));
    }

    [TestMethod]
    public void Reason_PlaceholdersIndicesWrongOutOfRange_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("{0} {2}", "10", "20"));
    }

    [TestMethod]
    public void Reason_NegativePlaceholderIndex_Throws()
    {
        Should.Throw<ArgumentException>(() => new Reason("Error {-1} occurred", "404"));
    }

    [TestMethod]
    public void Reason_ParametersWithNullElements_ShouldHandleGracefully()
    {
        var reason = new Reason("{0} and {1}", "Value", null);

        reason.ToString().ShouldBe("Value and ");
    }
}