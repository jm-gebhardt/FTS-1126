using System.Collections.ObjectModel;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using MassTransit;
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
            { agvCorrelationId1, CargoGenerator.GenerateCargos(agvCorrelationId1) },
            { agvCorrelationId2, CargoGenerator.GenerateCargos(agvCorrelationId2) },
            { agvCorrelationId3, CargoGenerator.GenerateCargos(agvCorrelationId3) }
        };
        CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .WithCargos(expectedCargosByAgvCorrelationId)
            .Build();

        Response<GotCargosOfAgvs> response = await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> { agvCorrelationId1, agvCorrelationId2, agvCorrelationId3 }));
        List<AgvCargos> agvsCargoList = response.Message.AgvCargosList.ToList();

        agvsCargoList.Count.ShouldBe(expectedCargosByAgvCorrelationId.Count);
        foreach (var cargo in agvsCargoList)
        {
            VerifyCargos(cargo.Cargos.ToList(), expectedCargosByAgvCorrelationId[cargo.CorrelationId]);
        }
    }

    [TestMethod]
    public async Task GetCargosOfAgvs_WhenAgvRepositoryThrows_ThrowsException()
    {
        CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetCargosOfAgvs, CargoRequestHandler>()
            .Build();

        fixture.AgvDb.Setup(x => x.Cargos.GetCargosAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ICollection<Guid> correlationIds, CancellationToken _) => throw new AgvsNotFoundException(correlationIds));

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetCargosOfAgvs(new List<Guid> { Guid.NewGuid() })));
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvHasCargo_ReturnsTrue()
    {
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, cargos)
            .Build();

        Response<GotAgvHasCargo> response = await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(agvCorrelationId));

        response.Message.AgvHasCargo.HasCargo.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvDoesNotHaveCargo_ReturnsFalse()
    {
        var agvCorrelationId = Guid.NewGuid();
        CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationId, new Collection<Cargo>())
            .Build();

        Response<GotAgvHasCargo> response = await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(agvCorrelationId));

        response.Message.AgvHasCargo.HasCargo.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAgvHasCargo_WhenAgvDoesNotExist_ThrowsException()
    {
        CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetAgvHasCargo, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvHasCargo>(new GetAgvHasCargo(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task GetAgvsHasCargo_WhenAgvsExist_ReturnsGotGetAgvsHasCargo()
    {
        var agvCorrelationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationIds[0]);
        CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .WithCargo(agvCorrelationIds[0], cargos)
            .WithCargo(agvCorrelationIds[1], new Collection<Cargo>())
            .Build();

        Response<GotAgvsHasCargo> response = await fixture.GetResponseAsync<GotAgvsHasCargo>(new GetAgvsHasCargo(agvCorrelationIds));

        List<AgvHasCargo> agvsHaveCargo = response.Message.AgvsHasCargo;
        agvsHaveCargo.Count.ShouldBe(2);
        agvsHaveCargo[0].CorrelationId.ShouldBe(agvCorrelationIds[0]);
        agvsHaveCargo[0].HasCargo.ShouldBeTrue();
        agvsHaveCargo[1].CorrelationId.ShouldBe(agvCorrelationIds[1]);
        agvsHaveCargo[1].HasCargo.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAgvsHasCargo_WhenAgvDoesNotExist_ThrowsException()
    {
        CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler> fixture = new CargoRequestHandlerTestFixture<GetAgvsHasCargo, CargoRequestHandler>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotCargosOfAgvs>(new GetAgvsHasCargo(new List<Guid> { Guid.NewGuid() })));
    }
}