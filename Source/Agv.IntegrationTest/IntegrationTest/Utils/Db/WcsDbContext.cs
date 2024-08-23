using Gebhardt.Acc.IntegrationTest.Utils.Db.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Utils.Db;

/// <summary>
/// The Wcs Database Context
/// </summary>
public class WcsDbContext : DbContext
{
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
    /// Create with default password
    /// 
    /// </summary>
    public WcsDbContext(): base(
        new DbContextOptionsBuilder()
            .UseSqlServer(
                $"Server=127.0.0.1;Database=master;User Id=sa;Password={EnvFile.DefaultPassword};Encrypt=False;MultipleActiveResultSets=True"
            )
            .Options
    )
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

    /// <summary>
    /// Add new order into ToAgv
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="leNo"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public async Task AddNewOrder(string orderId, string leNo, string source, string destination)
    {
        await ToAgv.AddAsync(new ToAgv() {
            RecordType = RecordType.TransportOrder,
            MessageStatus = MessageStatus.Pending,
            OrderId = orderId,
            OrderType = OrderType.Transport,
            LeNo = leNo,
            Source = source,
            Destination = destination,
            Vehicle = null,
            Status = OrderStatus.Initial,
            Error = null,
            Created = DateTime.Now,
            CcuVersion = 0,
            TimeStamp = DateTime.Now
        });
    }
}