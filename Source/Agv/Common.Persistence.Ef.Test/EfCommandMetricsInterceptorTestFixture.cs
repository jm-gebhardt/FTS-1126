using System.Data.Common;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;

namespace Gebhardt.Acc.Common.Persistence.Ef.Test;

public class EfCommandMetricsInterceptorTestFixture
{
    private const string OperationLabel = "operation";
    private const string DurationMetricName = "ACC_EF_COMMAND_DURATION_SECONDS";
    private const string DurationHelp = "Duration of EF Core database command execution in seconds";
    private const string TimeoutMetricName = "ACC_EF_COMMAND_TIMEOUT_COUNT";
    private const string TimeoutHelp = "Number of EF Core database commands that failed with a SQL timeout";
    public const int SqlTimeoutErrorNumber = -2;
    public const int NonTimeoutErrorNumber = 1;

    private static readonly Histogram _duration = Metrics.CreateHistogram(DurationMetricName, DurationHelp, new HistogramConfiguration { LabelNames = [OperationLabel] });
    private static readonly Counter _timeouts = Metrics.CreateCounter(TimeoutMetricName, TimeoutHelp, new CounterConfiguration { LabelNames = [OperationLabel] });

    private readonly DbConnection _connection = new SqlConnection();
    private readonly DbCommand _command = new SqlCommand();

    public EfCommandMetricsInterceptor Interceptor { get; } = new();

    public long DurationCount(string operation)
    {
        return _duration.WithLabels(operation).Count;
    }

    public double DurationSum(string operation)
    {
        return _duration.WithLabels(operation).Sum;
    }

    public double TimeoutCount(string operation)
    {
        return _timeouts.WithLabels(operation).Value;
    }

    public CommandExecutedEventData ExecutedEvent(DbCommandMethod method, TimeSpan duration)
    {
        return new CommandExecutedEventData(null!, null!, _connection, _command, string.Empty, null, method, Guid.NewGuid(), Guid.NewGuid(), null, false, false, DateTimeOffset.UtcNow, duration, CommandSource.Unknown);
    }

    public CommandErrorEventData ErrorEvent(DbCommandMethod method, Exception exception)
    {
        return new CommandErrorEventData(null!, null!, _connection, _command, string.Empty, null, method, Guid.NewGuid(), Guid.NewGuid(), exception, false, false, DateTimeOffset.UtcNow, TimeSpan.Zero, CommandSource.Unknown);
    }

    public DbCommand Command()
    {
        return _command;
    }

    public static SqlException SqlException(int number)
    {
        var collectionConstructor = typeof(SqlErrorCollection).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        var errors = (SqlErrorCollection)collectionConstructor!.Invoke(null);

        var errorConstructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(c => c.GetParameters().Length >= 7 && c.GetParameters()[0].ParameterType == typeof(int));
        ParameterInfo[] parameters = errorConstructor.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        arguments[0] = number;
        arguments[1] = (byte)0;
        arguments[2] = (byte)20;
        arguments[3] = "server";
        arguments[4] = "Timeout expired";
        arguments[5] = string.Empty;
        arguments[6] = 0;
        for (int i = 7; i < parameters.Length; i++)
        {
            arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : DefaultValue(parameters[i].ParameterType);
        }
        object error = errorConstructor.Invoke(arguments);

        var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
        addMethod!.Invoke(errors, [error]);

        var createException = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic, null, [typeof(SqlErrorCollection), typeof(string)], null);
        return (SqlException)createException!.Invoke(null, [errors, "6.0"])!;
    }

    private static object? DefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}