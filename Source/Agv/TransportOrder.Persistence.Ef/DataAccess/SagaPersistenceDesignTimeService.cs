using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace TransportOrder.Persistence.Ef.DataAccess;


/// <inheritdoc />
[UsedImplicitly]
public class SagaPersistenceDesignTimeService : IDesignTimeServices
{
    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
    }
}