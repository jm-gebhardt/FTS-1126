using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Models.Agv;

[TestClass]
public class AgvStateTest
{
    [DataTestMethod]
    // From Initial
    [DataRow(AgvState.Initial, AgvState.Discovered)]
    // From Discovered
    [DataRow(AgvState.Discovered, AgvState.Idle)]
    // From Busy
    [DataRow(AgvState.Busy, AgvState.Idle)]
    [DataRow(AgvState.Busy, AgvState.Charging)]
    // From Idle
    [DataRow(AgvState.Idle, AgvState.Charging)]
    [DataRow(AgvState.Idle, AgvState.Busy)]
    // From Charging
    [DataRow(AgvState.Charging, AgvState.Idle)]
    [DataRow(AgvState.Charging, AgvState.Busy)]
    public void TransitionStateTo_ValidState_StateTransitioned(AgvState current, AgvState next)
    {
        var result = current.TransitionTo(next);
        result.ShouldBe(next);
    }

    [DataTestMethod]
    // From Initial
    [DataRow(AgvState.Initial, AgvState.Busy)]
    [DataRow(AgvState.Initial, AgvState.Charging)]
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