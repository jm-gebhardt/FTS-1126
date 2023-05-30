using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.CommonTest.Helpers;

public class AgvDummy : IAgv
{
    public Guid CorrelationId { get; }

    public string AgvId { get; }

    public string CurrentState { get; }

    public AgvStatus? AgvStatus { get; init; }

    public AgvGraph? AgvGraph { get; init; }

    public bool IsOnline { get; init; }

    public bool IsLocked { get; set; }

    public bool Assignable { get; init; }

    public DateTime TimeOfStateTransition { get; set; }

    public DateTime TimeOfLockStateChanged { get; set; }

    public AgvDummy(Guid correlationId, string agvId, string currentState)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        CurrentState = currentState;
    }
}