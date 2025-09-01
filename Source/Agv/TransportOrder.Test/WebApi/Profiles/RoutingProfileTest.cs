using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

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
        var route = new Route(Guid.NewGuid(), ["A", "B"], [Guid.NewGuid()]);
        var dto = _mapper.Map<RouteDto>(route);

        dto.ShouldNotBeNull();
        dto.CorrelationId.ShouldBe(route.CorrelationId);
        dto.Nodes.ShouldBe(route.Nodes);
        dto.Edges.ShouldBe(route.Edges);
    }
}