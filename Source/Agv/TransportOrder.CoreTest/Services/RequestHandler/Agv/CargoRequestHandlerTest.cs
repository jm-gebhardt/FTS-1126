using System.Collections.ObjectModel;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

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
        ICollection<Cargo> expectedCargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, expectedCargos)
            .Build();

        Response<GotCargosOfAgv> response = await fixture.GetResponseAsync<GotCargosOfAgv>(new GetCargosOfAgv(agvCorrelationId));

        VerifyCargos(response.Message.Cargos, expectedCargos);
    }

    [TestMethod]
    public async Task GetCargoOfAgv_WhenAgvDoesNotExist_ThrowsException()
    {
        var agvCorrelationId = Guid.NewGuid();
        CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgv, CargoRequestHandler>()
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
            {agvCorrelationId1, CargoGenerator.GenerateCargos(agvCorrelationId1)},
            {agvCorrelationId2, CargoGenerator.GenerateCargos(agvCorrelationId2)},
            {agvCorrelationId3, CargoGenerator.GenerateCargos(agvCorrelationId3)}
        };
        CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .WithCargos(expectedCargosByAgvCorrelationId)
            .Build();

        Response<GotCargosOfAgvs> response = await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> {agvCorrelationId1, agvCorrelationId2, agvCorrelationId3}));

        response.Message.CargosByAgvCorrelationId.Count.ShouldBe(expectedCargosByAgvCorrelationId.Count);
        foreach (KeyValuePair<Guid, ICollection<Cargo>> kv in response.Message.CargosByAgvCorrelationId)
        {
            VerifyCargos(kv.Value, expectedCargosByAgvCorrelationId[kv.Key]);
        }
    }

    [TestMethod]
    public async Task GetCargosOfAgvs_WhenAnyAgvDoesNotExist_ThrowsException()
    {
        var agvCorrelationId1 = Guid.NewGuid();
        var agvCorrelationId2 = Guid.NewGuid();
        var expectedCargosByAgvCorrelationId = new Dictionary<Guid, ICollection<Cargo>>
        {
            {agvCorrelationId1, CargoGenerator.GenerateCargos(agvCorrelationId1)},
            {agvCorrelationId2, CargoGenerator.GenerateCargos(agvCorrelationId2)}
        };
        CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .WithCargos(expectedCargosByAgvCorrelationId)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> {agvCorrelationId1, agvCorrelationId2, Guid.NewGuid()})));
    }

    [TestMethod]
    public async Task HasAgvCargo_WhenAgvHasCargo_ReturnsTrue()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, cargos)
            .Build();

        Response<HasAgvCargoResult> response = await fixture.GetResponseAsync<HasAgvCargoResult>(new HasAgvCargoRequest(agvCorrelationId));

        response.Message.HasAgvCargo.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasAgvCargo_WhenAgvDoesNotHaveCargo_ReturnsFalse()
    {
        var agvCorrelationId = Guid.NewGuid();
        CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, new Collection<Cargo>())
            .Build();

        Response<HasAgvCargoResult> response = await fixture.GetResponseAsync<HasAgvCargoResult>(new HasAgvCargoRequest(agvCorrelationId));

        response.Message.HasAgvCargo.ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasAgvCargo_WhenAgvDoesNotExist_ThrowsException()
    {
        CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<HasAgvCargoRequest, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<HasAgvCargoResult>(new HasAgvCargoRequest(Guid.NewGuid())));
    }
}