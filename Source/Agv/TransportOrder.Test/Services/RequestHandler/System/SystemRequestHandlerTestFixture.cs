using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.System;

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