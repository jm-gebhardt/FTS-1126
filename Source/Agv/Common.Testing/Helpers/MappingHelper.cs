using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Common.Testing.Helpers;

/// <summary>
/// Helper class for getting mapper configuration for the profiles provided
/// </summary>
public static class MappingHelper
{
    public static Mapper GetMapper(params Profile[] profiles)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
        
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            foreach (var profile in profiles)
            {
                cfg.AddProfile(profile);
            }
        }, loggerFactoryMock.Object);
        mapperConfig.AssertConfigurationIsValid();
        var mapper = new Mapper(mapperConfig);
        return mapper;
    }
}