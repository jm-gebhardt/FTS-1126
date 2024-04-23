using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.WCSCommunicationTest.Fixtures;

/// <summary>
///  MassTransit BackgroundService test fixture with EF DbContextFactory services added
/// The factory automatically adds an in memory database with random name
/// </summary>
public class EfPollableServiceTestFixture<TPollable, TDbContext> : PollableServiceTestFixture<TPollable>
    where TPollable : class, IPollable where TDbContext : DbContext
{
    private readonly Guid _id = Guid.NewGuid();

    /// <summary>
    /// DbContextFactory used to create TDbContext instances
    /// </summary>
    public readonly IDbContextFactory<TDbContext> DbContextFactory;

    /// <summary>
    /// MassTransit BackgroundService test fixture with EF DbContextFactory services added
    /// </summary>
    public EfPollableServiceTestFixture()
    {
        AddServices(s => s.AddTestingDb<TDbContext>(_id));
        Build();
        DbContextFactory = Provider.GetRequiredService<IDbContextFactory<TDbContext>>().EnsureDbCreated();
    }
}