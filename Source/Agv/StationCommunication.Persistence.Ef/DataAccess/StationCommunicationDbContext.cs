using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;

/// <summary>
/// Db Context for OrderTranslator
/// </summary>
public class StationCommunicationDbContext : BaseDbContext<StationCommunicationDbContext>, IDbContext<StationCommunicationDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "STATIONCOMMUNICATION";

    /// <inheritdoc />
    public StationCommunicationDbContext(DbContextOptions<StationCommunicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JamStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderReportEntityConfiguration());
    }
}