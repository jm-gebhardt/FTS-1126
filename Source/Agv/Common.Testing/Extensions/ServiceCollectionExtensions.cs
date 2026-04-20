using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class ServiceCollectionExtensions
{
    [PublicAPI]
    public static IServiceCollection AddTestingDb<TContext>(this IServiceCollection serviceCollection, Guid id = default) where TContext : DbContext
    {
        serviceCollection.AddDbContextFactory<TContext>(options =>
        {
            options.UseInMemoryDatabase((id == Guid.Empty ? Guid.NewGuid() : id).ToString())
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
        return serviceCollection;
    }
}