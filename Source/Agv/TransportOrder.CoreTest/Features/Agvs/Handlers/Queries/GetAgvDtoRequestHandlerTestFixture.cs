using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

public sealed class GetAgvDtoRequestHandlerTestFixture : AgvRequestsHandlerTestFixture<GetAgvDtoRequest, GetAgvDtoRequestHandler>
{
    public GetAgvDtoRequestHandlerTestFixture()
    {
        ConfigureAdditionalRequestClient<GetAgvsDtoRequestHandler, GetAgvsDtoRequest>();
    }
}