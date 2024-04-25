using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvErrorEntityConfiguration : IEntityTypeConfiguration<AgvError>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvError> builder)
    {
        builder.ToTable("StatisticsAgvErrors");
        builder.HasKey(e => e.ErrorId);
        builder.Property(e => e.PositionId);
        builder.HasOne(e => e.Position)
            .WithMany(p => p.AgvErrors) 
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.NoAction); // 
        builder.Navigation(e => e.Position).AutoInclude();
        builder.ConfigureAllProperties(except: new[] { nameof(AgvError.Position) });
    }
}