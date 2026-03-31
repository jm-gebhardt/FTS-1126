using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

internal class GraphLayoutRepositoryTestFixture : DeviceCommunicationVda5050DbFixture
{
    public IGraphLayoutRepository Repository { get; }

    public GraphLayoutRepositoryTestFixture()
    {
        Repository = new EfGraphLayoutRepository(ContextFactory);
    }
}