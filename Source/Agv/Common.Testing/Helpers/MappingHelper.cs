using AutoMapper;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Helper class for getting mapper configuration for the profiles provided
/// </summary>
public static class MappingHelper
{
    public static Mapper GetMapper(params Profile[] profiles)
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            foreach (var profile in profiles)
            {
                cfg.AddProfile(profile);
            }
        });
        mapperConfig.AssertConfigurationIsValid();
        var mapper = new Mapper(mapperConfig);
        return mapper;
    }
}