using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.OwnedResource;

/// <inheritdoc />
public class OwnedFilterTemplateEntityConfiguration : IEntityTypeConfiguration<OwnedFilterTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OwnedFilterTemplate> builder)
    {
        builder.HasKey(f => f.ResourceCorrelationId);

        builder.Property(f => f.Owner).HasMaxLength(DbStringLimits.User);

        builder.Property(f => f.ResourceName).HasMaxLength(DbStringLimits.Description);

        var jsonOptions = new JsonSerializerOptions { Converters = { new FilterJsonConverter() } };
        builder.Property(f => f.Resource).HasConversion(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<FilterTemplate>(v, jsonOptions)!
        );
    }
}