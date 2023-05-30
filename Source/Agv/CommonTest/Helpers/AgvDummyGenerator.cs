using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.CommonTest.Helpers;

public static class AgvDummyGenerator
{
    public static IAgv GenerateAgv(string agvId = "AGV000", string agvState = "Idle")
    {
        return new AgvDummy(Guid.NewGuid(), agvId, agvState);
    }
}