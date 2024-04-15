using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class CargoProfileTest
{
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereCargoIsMappedToCargoDto()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var mapper = MappingHelper.GetMapper(new CargoProfile());

        var cargoDtos = mapper.Map<List<CargoDto>>(cargos);

        foreach (var dtoCargo in cargoDtos)
        {
            var cargo = cargos.Single(q => q.CorrelationId == dtoCargo.CorrelationId);
            dtoCargo.ShouldSatisfyAllConditions(
                q => q.CorrelationId.ShouldBe(cargo.CorrelationId),
                q => q.CargoId.ShouldBe(cargo.LoadId),
                q => q.CargoType.ShouldBe(cargo.LoadType),
                q => q.CenterPointPosition.ShouldBe(cargo.CenterPointPosition),
                q => q.Dimension.ShouldBe(cargo.Dimension),
                q => q.CargoPosition.ShouldBe(cargo.LoadPosition),
                q => q.Weight.ShouldBe(cargo.Weight)
            );
        }
    }
}