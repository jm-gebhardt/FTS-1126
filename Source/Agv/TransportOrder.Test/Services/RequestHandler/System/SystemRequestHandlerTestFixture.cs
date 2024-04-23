using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.System;

public class SystemRequestHandlerTestFixture<TRequestClient> : MediatorTestFixture<TRequestClient, SystemRequestHandler>
    where TRequestClient : class
{
    public SystemRequestHandlerTestFixture<TRequestClient> WithCurrentTime(DateTime currentTime)
    {
        TimeProvider.SetNow(currentTime);
        return this;
    }

    public override SystemRequestHandlerTestFixture<TRequestClient> Build()
    {
        base.Build();
        return this;
    }
}