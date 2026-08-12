using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

public class AgvTypeProfileTestFixture
{
    public const string Manufacturer = "KARIS";
    public const string SeriesName = "BF1";
    public const string AgvTypeIdentifier = $"{Manufacturer}.{SeriesName}";
    public const double Length = 1.5;
    public const double Width = 0.8;
    public const bool Acknowledged = true;
    public const bool HasGraph = false;

    public IMapper Mapper { get; } = MappingHelper.GetMapper(new AgvTypeProfile());
}