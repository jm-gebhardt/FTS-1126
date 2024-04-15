using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvErrorEntityConfiguration : IEntityTypeConfiguration<AgvError>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvError> builder)
    {
        builder.HasKey(o => o.CorrelationId);
        builder.Property(o => o.ErrorReferences).HasJsonConversion();
    }
}