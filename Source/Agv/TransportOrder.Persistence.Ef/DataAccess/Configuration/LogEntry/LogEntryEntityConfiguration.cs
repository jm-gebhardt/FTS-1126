using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.LogEntry;

/// <inheritdoc cref="IEntityTypeConfiguration{T}"/>>
public class LogEntryEntityConfiguration: IEntityTypeConfiguration<Domain.Models.LogEntry.LogEntry>
{
    /// <inheritdoc cref="IEntityTypeConfiguration{T}"/>>
    public void Configure(EntityTypeBuilder<Domain.Models.LogEntry.LogEntry> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Required fields
        builder.Property(e => e.LogCode).IsRequired();
        builder.Property(e => e.Category).IsRequired().HasMaxLength(50);
        builder.Property(e => e.LogLevel).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Timestamp).IsRequired();

        // Optional correlation fields
        builder.Property(e => e.AgvCorrelationId);
        builder.Property(e => e.StationId);
        builder.Property(e => e.ActionGroupCorrelationId);

        // Table configuration
        builder.ToTable("LogEntries");

        // Single column indexes
        builder.HasIndex(e => e.LogCode).HasDatabaseName("IX_LogEntries_LogCode");
        builder.HasIndex(e => e.Category).HasDatabaseName("IX_LogEntries_Category");
        builder.HasIndex(e => e.LogLevel).HasDatabaseName("IX_LogEntries_LogLevel");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("IX_LogEntries_Timestamp");
        builder.HasIndex(e => e.AgvCorrelationId).HasDatabaseName("IX_LogEntries_AgvCorrelationId");
        builder.HasIndex(e => e.StationId).HasDatabaseName("IX_LogEntries_StationId");
        builder.HasIndex(e => e.ActionGroupCorrelationId).HasDatabaseName("IX_LogEntries_ActionGroupCorrelationId");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.Category, e.Timestamp }).HasDatabaseName("IX_LogEntries_Category_Timestamp");
        builder.HasIndex(e => new { e.LogCode, e.Timestamp }).HasDatabaseName("IX_LogEntries_LogCode_Timestamp");
        builder.HasIndex(e => new { e.AgvCorrelationId, e.Timestamp }).HasDatabaseName("IX_LogEntries_AgvCorrelationId_Timestamp");
        builder.HasIndex(e => new { e.StationId, e.Timestamp }).HasDatabaseName("IX_LogEntries_StationId_Timestamp");

        builder.Property(e => e.Variables)
            .HasJsonConversion();
    }
}