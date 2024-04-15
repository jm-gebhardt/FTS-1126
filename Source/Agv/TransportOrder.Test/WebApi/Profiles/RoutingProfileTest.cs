using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class RoutingProfileTest
{
    private Mapper _mapper = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mapper = MappingHelper.GetMapper(new RoutingProfile());
    }

    [TestMethod]
    public void AssertConfigurationIsValid_DoesNotThrowException_WhenMappingProfileIsDefined()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereMapMessageContentIsMappedToDtoMap()
    {
        var route = new Route(Guid.NewGuid(), new[] { "A", "B" });
        var dto = _mapper.Map<RouteDto>(route);

        dto.ShouldNotBeNull();
        dto.CorrelationId.ShouldBe(route.CorrelationId);
        dto.Nodes.ShouldBe(route.Nodes);
    }
}