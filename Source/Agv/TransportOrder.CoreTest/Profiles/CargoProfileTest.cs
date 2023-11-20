using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class CargoProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereCargoIsMappedToDomainCargo()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Common.Messages.AgvManagement.Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var mapper = MappingHelper.GetMapper(new CargoProfile());
        var domainCargos = mapper.Map<List<Cargo>>(cargos);

        foreach (var domainCargo in domainCargos)
        {
            var cargo = cargos.Single(q => q.CorrelationId == domainCargo.CorrelationId);
            domainCargo.ShouldSatisfyAllConditions(
                q => q.CorrelationId.ShouldBe(cargo.CorrelationId),
                q => q.LoadId.ShouldBe(cargo.LoadId),
                q => q.LoadType.ShouldBe(cargo.LoadType),
                q => q.CenterPointPosition.ShouldBe(cargo.CenterPointPosition),
                q => q.Dimension.ShouldBe(cargo.Dimension),
                q => q.LoadPosition.ShouldBe(cargo.LoadPosition),
                q => q.Weight.ShouldBe(cargo.Weight)
            );
        }
    }
}