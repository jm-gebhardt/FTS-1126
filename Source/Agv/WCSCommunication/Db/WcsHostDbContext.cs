using Gebhardt.StoreWare.Agv.WCSCommunication.Db.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Db;

/// <summary>
/// The WCS database context (connection to host database)
/// </summary>
public class WcsHostDbContext : DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for host database is located
    /// </summary>
    public const string ConfigSectionName = "WCS";

    /// <summary>
    /// Get all Orders with <see cref="MessageStatus.Pending" />
    /// </summary>
    private IQueryable<ToAgv> PendingOrders => 
        from b in ToAgv
        where
            b.MessageStatus == MessageStatus.Pending 
        orderby b.TimeStamp
        select b;

    /// <summary>
    /// Get all New Orders
    /// </summary>
    public IQueryable<ToAgv> NewOrders =>
        from b in PendingOrders
        where
            b.RecordType == RecordType.TransportOrder
        select b;

    /// <summary>
    /// Get all Orders supposed to be cancelled
    /// </summary>
    public IQueryable<ToAgv> CancelRequests =>
        from b in ToAgv
        where
            b.RecordType == RecordType.OrderCancelRequest &&
            b.MessageStatus == MessageStatus.Pending
        orderby b.TimeStamp
        select b;

    /// <summary>
    /// Returns latest entry from ToAgv with given orderId
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public ToAgv? LatestOrderStatus(string orderId) => 
        ToAgv.Where(b => 
            b.OrderId == orderId
        ).OrderByDescending(b => b.TimeStamp).FirstOrDefault();

    /// <summary>
    ///  FromAgv DataSet
    /// </summary>
    [PublicAPI]
    public DbSet<FromAgv> FromAgv { get; set; } = null!;
    /// <summary>
    /// ToAgv DataSet
    /// </summary>
    [PublicAPI]
    public DbSet<ToAgv> ToAgv { get; set; }  = null!;
        
    /// <summary>
    /// WCSDbContext constructor with options
    /// </summary>
    /// <param name="options"></param>
    public WcsHostDbContext(DbContextOptions<WcsHostDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configure the DbContext
    /// The default EF Logging is very chatty and prints each CommandExecution using LogLevel.Information.
    /// This configuration changes this to LogLevel.Debug
    /// Source: https://github.com/dotnet/efcore/issues/14523#issuecomment-491532384
    /// </summary>
    /// <param name="optionsBuilder"></param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(c =>
        {
            // Logs from Microsoft.EntityFrameworkCore.Database.Command
            c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug));
            // Logs from Microsoft.EntityFrameworkCore.Infrastructure
            c.Log((CoreEventId.ContextInitialized, LogLevel.Debug));
        });
    }
}