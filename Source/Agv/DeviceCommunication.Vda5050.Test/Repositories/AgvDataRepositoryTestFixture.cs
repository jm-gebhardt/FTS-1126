using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

public class AgvDataRepositoryTestFixture : DeviceCommunicationVda5050DbFixture
{
    public readonly IAgvDataRepository Repository;

    public AgvDataRepositoryTestFixture()
    {
        Repository = new EfAgvDataRepository(ContextFactory);
    }
}