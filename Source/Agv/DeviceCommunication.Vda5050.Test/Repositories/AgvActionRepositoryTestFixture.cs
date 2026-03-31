using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

public class AgvActionRepositoryTestFixture : DeviceCommunicationVda5050DbFixture
{
    public readonly IAgvActionRepository Repository;

    public AgvActionRepositoryTestFixture()
    {
        Repository = new EfAgvActionRepository(ContextFactory);
    }
}