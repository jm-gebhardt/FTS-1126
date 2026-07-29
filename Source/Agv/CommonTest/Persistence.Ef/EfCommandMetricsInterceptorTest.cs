using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Persistence.Ef;

[TestClass]
[DoNotParallelize]
public class EfCommandMetricsInterceptorTest
{
    [DataRow(DbCommandMethod.ExecuteReader, "ExecuteReader")]
    [DataRow(DbCommandMethod.ExecuteScalar, "ExecuteScalar")]
    [DataRow(DbCommandMethod.ExecuteNonQuery, "ExecuteNonQuery")]
    [TestMethod]
    public void Executed_RecordsDurationForOperation(DbCommandMethod method, string operation)
    {
        var fixture = new EfCommandMetricsInterceptorTestFixture();
        var duration = TimeSpan.FromMilliseconds(250);
        const double tolerance = 0.0001;
        long countBefore = fixture.DurationCount(operation);
        double sumBefore = fixture.DurationSum(operation);
        var eventData = fixture.ExecutedEvent(method, duration);

        switch (method)
        {
            case DbCommandMethod.ExecuteReader:
                fixture.Interceptor.ReaderExecuted(fixture.Command(), eventData, null!);
                break;

            case DbCommandMethod.ExecuteScalar:
                fixture.Interceptor.ScalarExecuted(fixture.Command(), eventData, null);
                break;

            case DbCommandMethod.ExecuteNonQuery:
                fixture.Interceptor.NonQueryExecuted(fixture.Command(), eventData, 0);
                break;
        }

        fixture.DurationCount(operation).ShouldBe(countBefore + 1);
        fixture.DurationSum(operation).ShouldBe(sumBefore + duration.TotalSeconds, tolerance);
    }

    [TestMethod]
    public async Task ReaderExecutedAsync_RecordsDuration()
    {
        var fixture = new EfCommandMetricsInterceptorTestFixture();
        var duration = TimeSpan.FromMilliseconds(500);
        const string operation = "ExecuteReader";
        long countBefore = fixture.DurationCount(operation);

        await fixture.Interceptor.ReaderExecutedAsync(fixture.Command(), fixture.ExecutedEvent(DbCommandMethod.ExecuteReader, duration), null!);

        fixture.DurationCount(operation).ShouldBe(countBefore + 1);
    }

    [TestMethod]
    public void CommandFailed_SqlTimeout_IncrementsTimeoutCounter()
    {
        var fixture = new EfCommandMetricsInterceptorTestFixture();
        const string operation = "ExecuteReader";
        double before = fixture.TimeoutCount(operation);
        var timeout = EfCommandMetricsInterceptorTestFixture.SqlException(EfCommandMetricsInterceptorTestFixture.SqlTimeoutErrorNumber);

        fixture.Interceptor.CommandFailed(fixture.Command(), fixture.ErrorEvent(DbCommandMethod.ExecuteReader, timeout));

        fixture.TimeoutCount(operation).ShouldBe(before + 1);
    }

    [TestMethod]
    public void CommandFailed_NonTimeoutSqlException_DoesNotIncrement()
    {
        var fixture = new EfCommandMetricsInterceptorTestFixture();
        const string operation = "ExecuteScalar";
        double before = fixture.TimeoutCount(operation);
        var sqlError = EfCommandMetricsInterceptorTestFixture.SqlException(EfCommandMetricsInterceptorTestFixture.NonTimeoutErrorNumber);

        fixture.Interceptor.CommandFailed(fixture.Command(), fixture.ErrorEvent(DbCommandMethod.ExecuteScalar, sqlError));

        fixture.TimeoutCount(operation).ShouldBe(before);
    }

    [TestMethod]
    public void CommandFailed_NonSqlException_DoesNotIncrement()
    {
        var fixture = new EfCommandMetricsInterceptorTestFixture();
        const string operation = "ExecuteNonQuery";
        double before = fixture.TimeoutCount(operation);

        fixture.Interceptor.CommandFailed(fixture.Command(), fixture.ErrorEvent(DbCommandMethod.ExecuteNonQuery, new InvalidOperationException()));

        fixture.TimeoutCount(operation).ShouldBe(before);
    }
}