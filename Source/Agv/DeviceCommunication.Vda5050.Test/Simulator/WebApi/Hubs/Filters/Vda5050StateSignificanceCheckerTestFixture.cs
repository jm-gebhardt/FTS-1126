using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Hubs.Filters;

public class Vda5050StateSignificanceCheckerTestFixture
{
    public const double BatteryChargeThreshold = 1.0;
    public const double PositionThreshold = 0.05;
    public const double ThetaThreshold = 0.1;
    public const double InsignificantDelta = 0.001;
    public const double BaselineBatteryCharge = 80.0;

    public Vda5050StateSignificanceChecker Checker { get; }

    public Vda5050StateSignificanceCheckerTestFixture()
    {
        var configuration = new Vda5050StateConfiguration
        {
            BatteryChargeThreshold = BatteryChargeThreshold,
            PositionThreshold = PositionThreshold,
            ThetaThreshold = ThetaThreshold
        };
        Checker = new Vda5050StateSignificanceChecker(configuration);
    }
}