using System.Collections.ObjectModel;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class CargoRequestHandlerTest
{
    private static void VerifyCargos(ICollection<Cargo> actualCargos, ICollection<Cargo> expectedCargos)
    {
        foreach (var cargo in actualCargos)
        {
            var expectedCargo = expectedCargos.Single(q => q.CorrelationId == cargo.CorrelationId);
            cargo.ShouldSatisfyAllConditions(
                q => q.CorrelationId.ShouldBe(expectedCargo.CorrelationId),
                q => q.LoadId.ShouldBe(expectedCargo.LoadId),
                q => q.LoadType.ShouldBe(expectedCargo.LoadType),
                q => q.CenterPointPosition.ShouldBe(expectedCargo.CenterPointPosition),
                q => q.Dimension.ShouldBe(expectedCargo.Dimension),
                q => q.LoadPosition.ShouldBe(expectedCargo.LoadPosition),
                q => q.Weight.ShouldBe(expectedCargo.Weight)
            );
        }
    }

    [TestMethod]
    public async Task GetCargoOfAgv_ReturnsCargos()
    {
        var agvCorrelationId = Guid.NewGuid();
        var expectedCargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, expectedCargos)
            .Build();

        var response = await fixture.GetResponseAsync<GotCargosOfAgv>(new GetCargosOfAgv(agvCorrelationId));

        VerifyCargos(response.Message.Cargos, expectedCargos);
    }

    [TestMethod]
    public async Task GetCargoOfAgv_WhenAgvDoesNotExist_ThrowsException()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgv>(new GetCargosOfAgv(agvCorrelationId)));
    }

    [TestMethod]
    public async Task GetCargosOfAgvs_ReturnsCargosByAgvCorrelationId()
    {
        var agvCorrelationId1 = Guid.NewGuid();
        var agvCorrelationId2 = Guid.NewGuid();
        var agvCorrelationId3 = Guid.NewGuid();
        var expectedCargosByAgvCorrelationId = new Dictionary<Guid, ICollection<Cargo>>
        {
            { agvCorrelationId1, CargoGenerator.GenerateCargos(agvCorrelationId1) },
            { agvCorrelationId2, CargoGenerator.GenerateCargos(agvCorrelationId2) },
            { agvCorrelationId3, CargoGenerator.GenerateCargos(agvCorrelationId3) }
        };
        var fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .WithCargos(expectedCargosByAgvCorrelationId)
            .Build();

        var response = await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> { agvCorrelationId1, agvCorrelationId2, agvCorrelationId3 }));
        var agvsCargoList = response.Message.AgvCargosList.ToList();

        agvsCargoList.Count.ShouldBe(expectedCargosByAgvCorrelationId.Count);
        foreach (var cargo in agvsCargoList)
        {
            VerifyCargos(cargo.Cargos.ToList(), expectedCargosByAgvCorrelationId[cargo.CorrelationId]);
        }
    }

    [TestMethod]
    public async Task GetCargosOfAgvs_WhenAgvRepositoryThrows_ThrowsException()
    {
        var fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .Build();

        fixture.AgvDb.Setup(x => x.Cargos.GetCargosAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync((ICollection<Guid> correlationIds) => throw new AgvsNotFoundException(correlationIds));

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> { Guid.NewGuid() })));
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvHasCargo_ReturnsTrue()
    {
        var agvCorrelationId = Guid.NewGuid();
        var cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, cargos)
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(agvCorrelationId));

        response.Message.AgvHasCargo.HasCargo.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvDoesNotHaveCargo_ReturnsFalse()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, new Collection<Cargo>())
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(agvCorrelationId));

        response.Message.AgvHasCargo.HasCargo.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvDoesNotExist_ThrowsException()
    {
        var fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task GetAgvsHasCargo_WhenAgvsExist_ReturnsGotGetAgvsHasCargo()
    {
        var agvCorrelationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var cargos = CargoGenerator.GenerateCargos(agvCorrelationIds[0]);
        var fixture = new CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationIds[0], cargos)
            .WithCargo(agvCorrelationIds[1], new Collection<Cargo>())
            .Build();

        var response = await fixture.GetResponseAsync<GotAgvsHasCargo>(new GetAgvsHasCargo(agvCorrelationIds));

        var agvsHaveCargo = response.Message.AgvsHasCargo;
        agvsHaveCargo.Count.ShouldBe(2);
        agvsHaveCargo[0].CorrelationId.ShouldBe(agvCorrelationIds[0]);
        agvsHaveCargo[0].HasCargo.ShouldBeTrue();
        agvsHaveCargo[1].CorrelationId.ShouldBe(agvCorrelationIds[1]);
        agvsHaveCargo[1].HasCargo.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAgvsHasCargo_WhenAgvDoesNotExist_ThrowsException()
    {
        var fixture = new CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetAgvsHasCargo(new List<Guid> { Guid.NewGuid() })));
    }
}