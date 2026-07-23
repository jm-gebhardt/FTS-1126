using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class RouteExecutionConfiguration : IEntityTypeConfiguration<RouteExecution>
{
    private const string TableName = "DeviceCommunicationVda5050RouteExecution";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RouteExecution> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.RouteId);
        builder.Property(e => e.OrderIdHistory)
            .HasConversion(EfValueConverters.CreateGuidListConverter())
            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<Guid>());

        builder.OwnsMany(e => e.Steps, RouteStepExecutionConfiguration.Configure);
    }
}
