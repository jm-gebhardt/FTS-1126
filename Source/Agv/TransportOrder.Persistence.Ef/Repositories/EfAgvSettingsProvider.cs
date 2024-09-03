using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Agv settings provider that uses Ef to store data.
/// </summary>
public class EfAgvSettingsProvider : IAgvSettingsProvider
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> _agvSettingsProvider;

    /// <summary>
    /// Default settings.
    /// </summary>
    public static AgvSettingsReadModel DefaultSettings { get; } = new(15000, 20000, 20, 40, 60, 80);

    /// <summary>
    /// Initializes a new <see cref="EfAgvSettingsProvider"/> instance.
    /// </summary>
    public EfAgvSettingsProvider(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> agvSettingsProvider)
    {
        _dbContextFactory = dbContextFactory;
        _agvSettingsProvider = agvSettingsProvider;
    }

    /// <inheritdoc />
    public AgvSettingsReadModel GetSettingsForAgvGroup(Guid agvSettingsGroup)
    {
        if (_agvSettingsProvider.ConcurrentDict.TryGetValue(agvSettingsGroup, out var settings))
        {
            return settings;
        }

        throw new AgvSettingsNotFoundException(agvSettingsGroup);
    }

    /// <inheritdoc />
    public async Task<AgvSettingsGroup> GetSettingsGroupForAgv(Domain.Models.Agv.Agv? agv = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var group = await context.Set<AgvSettingsGroup>()
            .Where(g => (g.GroupType == GroupType.SerialNumber && agv != null && agv.SerialNumber == g.Identifier) ||
                        g.GroupType == GroupType.All)
            .OrderByDescending(ag => ag.GroupType)
            .Take(1)
            .SingleOrDefaultAsync();
        return group ?? throw new AgvSettingsNotFoundException();
    }

    /// <inheritdoc />
    public async Task AddOrUpdateSettingsForAgvGroup(AgvSettingsReadModel newSettings, GroupType type, string identifier)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var defaultGroup = await GetDefaultGroup();
        var group = await context.Set<AgvSettingsGroup>().SingleOrDefaultAsync(g => g.GroupType == type && g.Identifier == identifier);
        
        // Add new settings group
        if (group == null)
        {
            group = new AgvSettingsGroup(type, identifier);
            group.AgvSetting.ReplaceAllSettings(defaultGroup.AgvSetting);
            context.Set<AgvSettingsGroup>().Add(group);
           
            // Prevent settings to be added as new by marking them modified
            foreach (var setting in group.AgvSetting.GetAllSettings())
            {
                if (setting == null)
                {
                    continue;
                }
                context.Entry(setting).State = EntityState.Modified;
            }
        }

        group.UpdateSettings(newSettings);

        // Add or update settings
        foreach ((string name, object? _) in newSettings.GetValidPropertiesInformation())
        {
            var setting = group.AgvSetting.GetSettingByName(name);
            if (context.Set<Setting>().FirstOrDefault(s => s.Id == setting.Id) == null)
            {
                context.Set<Setting>().Add(setting);
                continue;
            }
            context.Set<Setting>().Update(setting);
        }
        await context.SaveChangesAsync();
        await ReloadInMemorySettings();
    }

    /// <inheritdoc />
    public async Task ResetSettingPropertyToDefaultAsync(Guid agvSettingsGroup, string propertyName)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var group = await context.Set<AgvSettingsGroup>()
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.Id == agvSettingsGroup);
        if (group == null)
        {
            throw new AgvSettingsNotFoundException();
        }

        var defaultGroup = await GetDefaultGroup();
        var defaultSetting = defaultGroup.AgvSetting.GetSettingByName(propertyName);
        var originalSetting = group.AgvSetting.GetSettingByName(propertyName);
       
        // if setting is already default do not do anything
        if (originalSetting.Id == defaultSetting.Id)
        {
            return;
        }
       
        group.AgvSetting.ReplaceSetting(defaultSetting);
        context.Set<AgvSettingsGroup>().Update(group);
        context.Set<Setting>().Remove(originalSetting);
        await context.SaveChangesAsync();
        // ReSharper disable once DisposeOnUsingVariable
        await context.DisposeAsync();
        await ReloadInMemorySettings();
    }

    private async Task<AgvSettingsGroup> GetDefaultGroup()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvSettingsGroup>()
                   .AsNoTracking()
                   .SingleOrDefaultAsync(g => g.GroupType == GroupType.All) ??
               await CreateDefaultGroup();
    }

    private void StoreInMemory(AgvSettingsGroup group, AgvSettingsReadModel newSettings)
    {
        _agvSettingsProvider.ConcurrentDict.AddOrUpdate(group.Id, newSettings, (_, _)=> newSettings);
    }

    private async Task<ICollection<AgvSettingsGroup>> ReloadInMemorySettings()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvSettingsGroup> groups = await context.Set<AgvSettingsGroup>().ToListAsync();
        foreach (var group in groups)
        {
            var settings = group.AgvSetting.ToReadModel();
            StoreInMemory(group, settings);
        }
        return groups;
    }

    private async Task<AgvSettingsGroup> CreateDefaultGroup()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var defaultGroup = new AgvSettingsGroup(GroupType.All);
        defaultGroup.AgvSetting.Update(DefaultSettings, true);
        context.Set<AgvSettingsGroup>().Add(defaultGroup);
        await context.SaveChangesAsync();
        StoreInMemory(defaultGroup, DefaultSettings);
        return defaultGroup;
    }

    /// <inheritdoc />
    public async Task InitializeSettingsGroups()
    {
        ICollection<AgvSettingsGroup> groups = await ReloadInMemorySettings();

        if (groups.All(g => g.GroupType != GroupType.All))
        {
            await CreateDefaultGroup();
        }
    }
}