using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

public class HostOrderStateMachineFixture : BaseStateMachineTestFixture<HostOrderStateMachine, HostOrderSaga>
{
    public HostOrderStateMachineFixture()
    {
        Build();
    }
}