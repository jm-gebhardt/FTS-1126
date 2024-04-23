using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfAgvSettingsProviderTestFixture
{
    private readonly List<Task> _tasks = [];

    public EfAgvSettingsProvider Repository { get; }

    public readonly InMemoryAgvSettingsProvider InMemorySettingsCache = new();

    private readonly IDbContextFactory<TransportOrderDbContext> _contextFactory;

    public EfAgvSettingsProviderTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        _contextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        Repository = new EfAgvSettingsProvider(_contextFactory, InMemorySettingsCache);
        _tasks.Add(Repository.InitializeSettingsGroups());
    }

    public EfAgvSettingsProviderTestFixture WithAgvSettingsGroup(AgvSettingsReadModel newSettings, GroupType type, string groupValue)
    {
        _tasks.Add(Repository.AddOrUpdateSettingsForAgvGroup(newSettings, type, groupValue));
        return this;
    }

    public async Task<EfAgvSettingsProviderTestFixture> BuildAsync()
    {
        await Task.WhenAll(_tasks);
        return this;
    }

    public async Task<ICollection<AgvSettingsGroup>> GetAddedAgvGroups(GroupType type, string groupValue)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<AgvSettingsGroup>().Where(a => a.GroupType == type && a.Identifier == groupValue).ToListAsync();
    }

    public async Task ResetDb()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        List<AgvSettingsGroup> groups = await context.Set<AgvSettingsGroup>().ToListAsync();
        context.RemoveRange(groups);
        await context.SaveChangesAsync();
    }
}