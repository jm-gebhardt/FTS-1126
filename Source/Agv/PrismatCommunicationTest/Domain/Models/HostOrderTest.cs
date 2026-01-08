using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class HostOrderTest
{
    [TestMethod]
    public void Finish_ValidData_DatesSet()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.Finish(finishTime);

        hostOrder.FinishedDate.ShouldBe(finishTime);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).FinishedDate.ShouldBe(finishTime);
    }

    [TestMethod]
    public void FinishAction_ValidData_DateSet()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.FinishAction(finishTime, ActionType.Pick);

        hostOrder.FailedDate.ShouldBeNull();
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).FinishedDate.ShouldBe(finishTime);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).FinishedDate.ShouldBeNull();
    }

    [TestMethod]
    public void FinishAction_AlreadyFinished_DateNotUpdated()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var finishTime = DateTime.UtcNow;
        hostOrder.FinishAction(finishTime, ActionType.Pick);
        hostOrder.FinishAction(DateTime.UtcNow, ActionType.Pick);
        hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).FinishedDate.ShouldBe(finishTime);
    }
}