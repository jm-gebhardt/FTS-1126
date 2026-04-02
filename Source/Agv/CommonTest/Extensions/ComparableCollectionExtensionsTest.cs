using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class ComparableCollectionExtensionsTest
{
    [TestMethod]
    public void Median_EvenLength_ReturnsMedian()
    {
        List<double> data = [2.0, 1.0, 3.0, 5.0];
        data.Median().ShouldBe((2.0 + 3.0) / 2.0);
    }

    [TestMethod]
    public void Median_OddLength_ReturnsMedian()
    {
        List<double> data = [2.0, 1.0, 3.0, 5.0, 6.0];
        data.Median().ShouldBe(3.0);
    }

    [TestMethod]
    public void Median_ComparableWithoutArithmeticOperators_ThrowsInvalidOperationException()
    {
        List<NotSupportedComparable> data = [new(ActionType.Drop), new(ActionType.Charge)];
        Should.Throw<InvalidOperationException>(data.Median);
    }

    /// <summary>
    /// Represents a class that implements <see cref="IComparable"/>,
    /// but does not support +/- operators.
    /// </summary>
    private class NotSupportedComparable : IComparable<NotSupportedComparable>
    {
        private ActionType Value { get; }

        /// <inheritdoc cref="NotSupportedComparable"/>>
        public NotSupportedComparable(ActionType value)
        {
            Value = value;
        }

        public int CompareTo(NotSupportedComparable? other)
        {
            if (other == null)
            {
                return 1;
            }
            return Value != other.Value ? 1 : 0;
        }
    }
}