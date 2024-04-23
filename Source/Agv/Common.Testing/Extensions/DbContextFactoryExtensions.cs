using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class DbContextFactoryExtensions
{
    public static IDbContextFactory<TDbContext> EnsureDbCreated<TDbContext>(this IDbContextFactory<TDbContext> factory) where TDbContext : DbContext
    {
        using var context = factory.CreateDbContext();
        context.Database.EnsureCreated();
        return factory;
    }
}