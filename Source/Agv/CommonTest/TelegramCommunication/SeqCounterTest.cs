using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.TelegramCommunication;

[TestClass]
public class SeqCounterTest
{
    [TestMethod]
    public void Construct_ValidData_WithinBounds()
    {
        const int start = 1;
        const int end = 2;
        var counter = new SeqCounter(start, end, 3);
        counter.Current.ShouldBe(start);
        counter.Previous.ShouldBe(end);
    }

    [TestMethod]
    public void ToString_ValidData_CorrectFormat()
    {
        var counter = new SeqCounter(1, 2, 3);
        
        counter.Current.ShouldBe(1);
        counter.ToString().ShouldBe("001");
    }

    [TestMethod]
    public void Current_ValueLargerThanEnd_SetsToStart()
    {
        const int start = 1;
        const int end = 2;
        var counter = new SeqCounter(start, end, 3)
        {
            Current = end + 1
        };
        counter.Current.ShouldBe(start);
    }

    [TestMethod]
    public void Increase_ValidData_CurrentSet()
    {
        const int start = 1;
        const int end = 5;
        var counter = new SeqCounter(start, end, 3);
        counter.Current.ShouldBe(start);
        counter.Previous.ShouldBe(end);

        counter.Increase();
        counter.Current.ShouldBe(start + 1);
        counter.Previous.ShouldBe(start);
    }
}