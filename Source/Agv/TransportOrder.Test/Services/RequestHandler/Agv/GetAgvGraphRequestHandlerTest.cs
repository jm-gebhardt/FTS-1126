using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvGraphRequestHandlerTest
{
    [TestMethod]
    public async Task GetAgvGraphRequest_AgvNotFound_ExceptionThrown()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraph, GetGraphRequestHandler>()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvGraph>(new GetAgvGraph(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task GetAgvGraphRequest_GraphNotFound_ExceptionThrown()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraph, GetGraphRequestHandler>()
            .WithEmptyGraphRepository()
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvGraphNotDefinedException>(async () => await fixture.GetResponseAsync<GotAgvGraph>(new GetAgvGraph(agv.CorrelationId)));
    }

    [TestMethod]
    public async Task Consume_GetAgvGraphs_AgvsNotFound_ThrowsAgvsNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphs, GetGraphRequestHandler>()
            .WithEmptyGraphRepository()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvGraphs>(new GetAgvGraphs(new List<Guid> { "INVALID".ToGuid() })));
    }

    [TestMethod]
    public async Task Consume_GetAgvGraphs_AgvsGraphNotDefined_DoesNotReturnGraph()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var fixture = new AgvRequestsHandlerTestFixture<GetAgvGraphs, GetGraphRequestHandler>()
            .WithEmptyGraphRepository()
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();

       var response = await fixture.GetResponseAsync<GotAgvGraphs>(new GetAgvGraphs(new List<Guid> { agv.CorrelationId }));

       response.Message.Graphs.ShouldNotBeNull();
       response.Message.Graphs.ShouldBeEmpty();
    }
}