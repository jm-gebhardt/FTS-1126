using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class LifFileEntityConfiguration: IEntityTypeConfiguration<LifFile>
{
    private const string TableName = "DeviceCommunicationVda5050Lif";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LifFile> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LayoutIds)
            .HasConversion(EfValueConverters.CreateJsonConverter<ICollection<Guid>>())
            .Metadata.SetValueComparer(new ValueComparer<ICollection<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(e => e.SupportedVehicleTypes)
            .HasConversion(EfValueConverters.CreateJsonConverter<ICollection<string>>())
            .Metadata.SetValueComparer(new ValueComparer<ICollection<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.OwnsOne(e => e.LifCollectionMetaInformation, metaBuilder =>
        {
            metaBuilder.Property(m => m.Creator).HasMaxLength(DbStringLimits.Name);
            metaBuilder.Property(m => m.LifVersion).HasMaxLength(DbStringLimits.Name);
            metaBuilder.Property(m => m.ProjectIdentification).HasMaxLength(DbStringLimits.Name);
        });

        builder.OwnsOne(e => e.MetaInformation, fileMetaBuilder =>
        {
            fileMetaBuilder.Property(m => m.Filename).HasMaxLength(DbStringLimits.Name);
            fileMetaBuilder.Property(m => m.UploadedBy).HasMaxLength(DbStringLimits.User);
            fileMetaBuilder.Property(m => m.ValidationResults).HasJsonConversion();
        });

        builder.Property(e => e.RawJson);
    }
}