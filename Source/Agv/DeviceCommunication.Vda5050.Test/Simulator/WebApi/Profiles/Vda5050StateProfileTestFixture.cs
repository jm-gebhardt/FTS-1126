using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles;

public class Vda5050StateProfileTestFixture
{
    public const string SerialNumber = "simagv1";
    public const double BatteryCharge = 55.5;
    public const double PositionX = 1.25;
    public const double Theta = 0.5;
    public const string NodeId = "n1";

    public IMapper Mapper { get; } = MappingHelper.GetMapper(new Vda5050StateProfile());
}