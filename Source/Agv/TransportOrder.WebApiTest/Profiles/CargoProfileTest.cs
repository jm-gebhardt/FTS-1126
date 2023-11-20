using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Profiles;

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