using AutoMapper;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

public class MappingProfileTestFixture
{
    public Mapper GetMapper(IEnumerable<Profile> profiles)
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