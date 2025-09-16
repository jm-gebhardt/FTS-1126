using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;

/// <summary>
/// 
/// </summary>
public class TriggerServiceDbContext : BaseDbContext<TriggerServiceDbContext>, IDbContext<TriggerServiceDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "TRIG";

    /// <inheritdoc />
    public TriggerServiceDbContext(DbContextOptions<TriggerServiceDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TriggerEntityConfiguration());
    }
}