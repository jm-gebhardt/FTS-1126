using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.EventHandlers;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class FactsheetEventHandlerTestFixture : MassTransitConsumerTestFixture<FactsheetEventHandler>
{
    public FactsheetEventHandlerTestFixture()
    {
        Build();
    }
}