using System.Collections.Immutable;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

[TestClass]
public class MapProfileTest
{
    /// <summary>
    /// </summary>
    [TestMethod]
    public void AssertConfigurationIsValid_DoesNotThrowException_WhenMappingProfileIsDefined()
    {
        var mapper = MappingHelper.GetMapper();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    /// <summary>
    /// Maps the <see cref="Map"/> to <see cref="MapDto"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereMapMessageContentIsMappedToDtoMap()
    {
        var mapper = MappingHelper.GetMapper(new MapProfile());
        var correlationId = Guid.NewGuid();

        var mapMessageContent = new Map(correlationId, "Area51_Floor1_Compartment47", 50.0d,0.01, 1, new[]
        {
            new Guid("3D3D5DA8-1A01-459E-B0DA-EBB875D393F6"),
            new Guid("A781AF33-CB57-4143-8EEF-1590FE9EC2ED")
        }, 10, 10);
        ImmutableDictionary<Guid, Guid[]> agvMapRelation = new Dictionary<Guid, Guid[]>
        {
            {
                correlationId,
               mapMessageContent.RelatedAgvs
            }
        }.ToImmutableDictionary(x => x.Key, x => x.Value);

        var dtoMap = mapper.Map<MapDto>(mapMessageContent, opt => opt.Items[RelatedMapAgvValueResolver.AgvMapRelationsName] = agvMapRelation);

        dtoMap.ShouldNotBeNull();
        dtoMap.CorrelationId.ShouldBe(mapMessageContent.CorrelationId);
        dtoMap.MapId.ShouldBe(mapMessageContent.MapId);
        dtoMap.Resolution.ShouldBe(mapMessageContent.Resolution);
        dtoMap.Resolution.ShouldBe(mapMessageContent.Resolution);
        dtoMap.XOffset.ShouldBe(mapMessageContent.XOffset);
        dtoMap.YOffset.ShouldBe(mapMessageContent.YOffset);
        dtoMap.RelatedAgvs.ShouldAllBe(p => agvMapRelation.Any(kvp => kvp.Key.Equals(dtoMap.CorrelationId) && kvp.Value.Contains(p)));
    }
}