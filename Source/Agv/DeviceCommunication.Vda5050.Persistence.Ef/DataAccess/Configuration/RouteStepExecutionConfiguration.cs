using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// Configures the owned <see cref="RouteStepExecution"/> collection of a <see cref="RouteExecution"/>.
/// </summary>
internal static class RouteStepExecutionConfiguration
{
    private const string TableName = "DeviceCommunicationVda5050RouteExecutionStep";

    /// <summary>
    /// Configure the owned navigation.
    /// </summary>
    public static void Configure(OwnedNavigationBuilder<RouteExecution, RouteStepExecution> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.RouteStepId);
        builder.Property(e => e.ActionAtEndNode).HasJsonConversion();
        builder.Property(e => e.FailureReason).HasMaxLength(DbStringLimits.Description);
    }
}
