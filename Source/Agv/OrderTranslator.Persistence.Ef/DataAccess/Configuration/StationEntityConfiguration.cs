using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class StationEntityConfiguration: IEntityTypeConfiguration<Station>
{
    private const string TableName = "OrderTranslatorStations";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(DbStringLimits.NodeId);

        builder.Property(s => s.NeedsCargoReporting);

        builder.Property(s => s.NeedsPalletReporting);

        builder.Property(s => s.PickDoppelspielMode)
            .HasConversion<string>();

        builder.Property(s => s.DropDoppelspielMode)
            .HasConversion<string>();

        builder.Property(s => s.PickRouting)
            .HasJsonConversion();

        builder.Property(s => s.DropRouting)
            .HasJsonConversion();

        builder.Property(s => s.StationGroup)
            .HasJsonConversion();

        builder.Property(s => s.InputLanes)
            .HasJsonConversion();

        builder.Property(s => s.OutputLanes)
            .HasJsonConversion();
    }
}