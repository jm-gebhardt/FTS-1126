using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvErrorEntityConfiguration : IEntityTypeConfiguration<AgvError>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvError> builder)
    {
        builder.HasKey(o => o.CorrelationId);
        builder.Property(o => o.ErrorReferences)
            .HasConversion(EfValueConverters.CreateJsonConverter<List<ErrorReference>?>())
            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<ErrorReference>());

        builder.HasOne<Agv>()
            .WithMany()
            .HasForeignKey(e => e.AgvCorrelationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(o => o.ErrorCode).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.ErrorType).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.ErrorDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.Source).HasConversion<string>().HasMaxLength(DbStringLimits.Enum);
        builder.Property(o => o.ErrorTitle).HasMaxLength(DbStringLimits.Name);

        builder.HasIndex(e => e.ErrorResolved)
            .HasFilter("[ErrorResolved] IS NULL");
    }
}