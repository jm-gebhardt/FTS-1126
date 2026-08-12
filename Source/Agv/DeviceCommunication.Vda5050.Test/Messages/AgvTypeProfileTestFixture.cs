using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

public class AgvTypeProfileTestFixture
{
    public const string Manufacturer = "KARIS";
    public const string SeriesName = "BF1";
    public const string AgvTypeIdentifier = $"{Manufacturer}.{SeriesName}";

    public IMapper Mapper { get; } = MappingHelper.GetMapper(new AgvTypeProfile());
}