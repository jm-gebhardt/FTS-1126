using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class LoadStateEntityConfiguration : IEntityTypeConfiguration<LoadState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LoadState> builder)
    {
        builder.ToTable("LoadState");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.IsLoaded);
        
        builder.Ignore(e => e.Description);
    }
}