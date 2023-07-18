using System.Collections.Immutable;
using System.Text;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

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
    public void Map_ShouldMapAllPropertiesCorrectly_WherePersistenceMapIsMappedToDtoMap()
    {
        var mapper = MappingHelper.GetMapper(new MapProfile());
        const string base64String = "dGhlIHF1aWNrIGJyb3duIGZveCBqdW1wIG92ZXIgdGhlIGxhenkgZG9n";
        var correlationId = Guid.NewGuid();

        var persistenceMap = new Map(correlationId, "Area51_Floor1_Compartment47", Encoding.UTF8.GetBytes(base64String), 0.01, 1, 2, DateTime.UtcNow);
        ImmutableDictionary<Guid, Guid[]> agvMapRelation = new Dictionary<Guid, Guid[]>
        {
            {
                correlationId,
                new[]
                {
                    new Guid("3D3D5DA8-1A01-459E-B0DA-EBB875D393F6"),
                    new Guid("A781AF33-CB57-4143-8EEF-1590FE9EC2ED")
                }
            }
        }.ToImmutableDictionary(x => x.Key, x => x.Value);

        var dtoMap = mapper.Map<MapDto>(persistenceMap, opt => opt.Items[RelatedMapAgvValueResolver.AgvMapRelationsName] = agvMapRelation);

        dtoMap.ShouldNotBeNull();
        dtoMap.CorrelationId.ShouldBe(persistenceMap.CorrelationId);
        dtoMap.MapId.ShouldBe(persistenceMap.MapId);
        dtoMap.Resolution.ShouldBe(persistenceMap.Resolution);
        dtoMap.Resolution.ShouldBe(persistenceMap.Resolution);
        dtoMap.XOffset.ShouldBe(persistenceMap.XOffset);
        dtoMap.YOffset.ShouldBe(persistenceMap.YOffset);
        dtoMap.RelatedAgvs.ShouldAllBe(p => agvMapRelation.Any(kvp => kvp.Key.Equals(dtoMap.CorrelationId) && kvp.Value.Contains(p)));
    }
}