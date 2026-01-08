using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess;

/// <summary>
/// Db Context for OrderTranslator
/// </summary>
public class OrderTranslatorDbContext : BaseDbContext<OrderTranslatorDbContext>, IDbContext<OrderTranslatorDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "ORDERTRANSLATOR";

    /// <inheritdoc />
    public OrderTranslatorDbContext(DbContextOptions<OrderTranslatorDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new HostOrderEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StationEntityConfiguration());
    }
}