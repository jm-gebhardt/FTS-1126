using System.Text.Json;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.OwnedResource;

/// <inheritdoc />
public class OwnedFilterEntityConfiguration : IEntityTypeConfiguration<OwnedFilter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OwnedFilter> builder)
    {
        builder.HasKey(f => f.ResourceCorrelationId);

        builder.Property(f => f.Owner).HasMaxLength(DbStringLimits.User);

        builder.Property(f => f.ResourceName).HasMaxLength(DbStringLimits.Description);

        var jsonOptions = new JsonSerializerOptions { Converters = { new FilterJsonConverter() } };
        builder.Property(f => f.Resource).HasConversion(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<FilterDto>(v, jsonOptions)!
        );
    }
}