using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// EF command interceptor that records database command execution duration and timeout counts as Prometheus metrics.
/// </summary>
[PublicAPI]
public class EfCommandMetricsInterceptor : DbCommandInterceptor
{
    private const string OperationLabel = "operation";

    private const int SqlTimeoutErrorNumber = -2;

    private static readonly Histogram _commandDuration = Metrics.CreateHistogram(
        "ACC_EF_COMMAND_DURATION_SECONDS",
        "Duration of EF Core database command execution in seconds",
        new HistogramConfiguration { LabelNames = [OperationLabel] });

    private static readonly Counter _commandTimeouts = Metrics.CreateCounter(
        "ACC_EF_COMMAND_TIMEOUT_COUNT",
        "Number of EF Core database commands that failed with a SQL timeout",
        new CounterConfiguration { LabelNames = [OperationLabel] });

    private static void RecordDuration(CommandExecutedEventData eventData)
    {
        _commandDuration.WithLabels(eventData.ExecuteMethod.ToString()).Observe(eventData.Duration.TotalSeconds);
    }

    private static void RecordFailure(CommandErrorEventData eventData)
    {
        if (eventData.Exception is SqlException { Number: SqlTimeoutErrorNumber })
        {
            _commandTimeouts.WithLabels(eventData.ExecuteMethod.ToString()).Inc();
        }
    }

    /// <inheritdoc/>
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        RecordDuration(eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        RecordDuration(eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        RecordDuration(eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        RecordDuration(eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        RecordFailure(eventData);
        base.CommandFailed(command, eventData);
    }

    /// <inheritdoc/>
    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        RecordFailure(eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }
}