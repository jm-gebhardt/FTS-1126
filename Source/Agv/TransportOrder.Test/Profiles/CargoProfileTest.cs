using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class CargoProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereCargoIsMappedToDomainCargo()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var mapper = MappingHelper.GetMapper(new CargoProfile());
        var domainCargos = mapper.Map<List<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);

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