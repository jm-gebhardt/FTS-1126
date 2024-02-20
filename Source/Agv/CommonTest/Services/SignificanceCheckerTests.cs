using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services;

[TestClass]
public class SignificanceCheckerTests
{
    [TestMethod]
    public void HasSignificantChanges_WhenPropertyChangesSignificantly_ReturnsTrue()
    {
        var checker = new TestMessageSignificanceChecker();
        var obj1 = new TestMessage { CustomProperty = 0, DefaultProperty = true };
        var obj2 = new TestMessage { CustomProperty = 11, DefaultProperty = true };

        checker.HasSignificantChanges(obj1, obj2).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenPropertyDoesNotChangeSignificantly_ReturnsFalse()
    {
        var checker = new TestMessageSignificanceChecker();
        var obj1 = new TestMessage { CustomProperty = 0, DefaultProperty = true };
        var obj2 = new TestMessage { CustomProperty = 1, DefaultProperty = true };

        checker.HasSignificantChanges(obj1, obj2).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenOneAnyOtherPropertyChanges_ReturnsTrue()
    {
        var checker = new TestMessageSignificanceChecker();
        var obj1 = new TestMessage { DefaultProperty = true };
        var obj2 = new TestMessage { DefaultProperty = false };

        checker.HasSignificantChanges(obj1, obj2).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenFirstObjectIsNull_ReturnsTrue()
    {
        var checker = new TestMessageSignificanceChecker();

        checker.HasSignificantChanges(null, new TestMessage()).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenLastObjectIsNull_ReturnsTrue()
    {
        var checker = new TestMessageSignificanceChecker();

        checker.HasSignificantChanges(new TestMessage(), null).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenBothObjectsAreNull_ReturnsTrue()
    {
        var checker = new TestMessageSignificanceChecker();

        checker.HasSignificantChanges(null, null).ShouldBeFalse();
    }
}