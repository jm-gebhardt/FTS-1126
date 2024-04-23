using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class ServiceCollectionExtensions
{
    [PublicAPI]
    public static IServiceCollection AddTestingDb<TContext>(this IServiceCollection serviceCollection, Guid id = default) where TContext : DbContext
    {
        serviceCollection.AddDbContextFactory<TContext>(options => { options.UseInMemoryDatabase((id = default ? Guid.NewGuid() : id).ToString()); });
        return serviceCollection;
    }
}