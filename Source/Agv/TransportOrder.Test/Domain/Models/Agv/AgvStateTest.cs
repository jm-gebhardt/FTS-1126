using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Agv;

[TestClass]
public class AgvStateTest
{
    [TestMethod]
    // From Initial
    [DataRow(AgvState.Initial, AgvState.Discovered)]
    // From Discovered
    [DataRow(AgvState.Discovered, AgvState.Idle)]
    // From Busy
    [DataRow(AgvState.Busy, AgvState.Idle)]
    [DataRow(AgvState.Idle, AgvState.Busy)]
    public void TransitionStateTo_ValidState_StateTransitioned(AgvState current, AgvState next)
    {
        var result = current.TransitionTo(next);
        result.ShouldBe(next);
    }

    [TestMethod]
    // From Initial
    [DataRow(AgvState.Initial, AgvState.Busy)]
    [DataRow(AgvState.Initial, AgvState.Idle)]
    // From Discovered
    [DataRow(AgvState.Discovered, AgvState.Busy)]
    // From Busy
    [DataRow(AgvState.Busy, AgvState.Busy)]
    public void TransitionStateTo_InvalidState_InvalidStateTransitionExceptionThrown(AgvState current, AgvState next)
    {
        Should.Throw<InvalidStateTransitionException>(() => current.TransitionTo(next));
    }
}