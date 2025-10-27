using Gebhardt.Acc.Common.Domain.Utilities;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Utilities;

[TestClass]
public class RandomAccessStackTest
{
    [TestMethod]
    public void IsBefore_ValidData_IsBefore()
    {
        var stack = new RandomAccessStack<int> { 1, 2, 3 };

        stack.IsBefore(1, 3).ShouldBeTrue();
        stack.IsBefore(1, 4).ShouldBeFalse();
        stack.IsBefore(4, 1).ShouldBeTrue();
    }

    [TestMethod]
    public void Push_ValidData_CorrectOrder()
    {
        var stack = new RandomAccessStack<int>();

        stack.Push(1);
        stack.Push(3);
        stack.Push(2);
        stack.Push(1);

        stack.Count.ShouldBe(3);
        stack.ToList().ShouldBe([1, 2, 3]);
    }

    [TestMethod]
    public void After_ValidData_ReturnsCorrectData()
    {
        var stack = new RandomAccessStack<int> { 2, 1, 3, 4 };

        List<int> result = stack.After(1).ToList();

        result.Count.ShouldBe(2);
        result.ShouldBe([3, 4]);
    }

    [TestMethod]
    public void Remove_ValidData_RemovesItems()
    {
        var stack = new RandomAccessStack<int> { 1, 2, 2, 3 };

        stack.Remove(2);

        stack.Count.ShouldBe(2);
        stack.ToList().ShouldBe([1, 3]);
    }
}