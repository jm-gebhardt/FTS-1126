using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using Moq;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation.Travel;

public class AgvTravelSimulatorMock
{
    private TimeSpan? _travelDuration = TimeSpan.Zero;
    private TimeSpan _pauseTime = TimeSpan.Zero;

    public Mock<IAgvTravelSimulator> Mock { get; }

    public IAgvTravelSimulator Object => Mock.Object;

    public AgvTravelSimulatorMock()
    {
        Mock = new Mock<IAgvTravelSimulator>();
        Mock.Setup(x => x.TravelDuration).Returns(() => _travelDuration);
        Mock.Setup(x => x.PauseTime).Returns(() => _pauseTime);
    }

    public AgvTravelSimulatorMock WithTravelDuration(TimeSpan travelDuration)
    {
        _travelDuration = travelDuration;
        return this;
    }

    public AgvTravelSimulatorMock WithPauseTime(TimeSpan pauseTime)
    {
        _pauseTime = pauseTime;
        return this;
    }
}