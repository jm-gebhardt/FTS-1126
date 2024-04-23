using Gebhardt.Acc.Common.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest;

/// <summary>
/// Test class for initial logging.
/// </summary>
[TestClass]
public class LoggingInitialTest
{
    /// <summary>
    /// Just check factory, we do not need to test ILogger.
    /// </summary>
    [TestMethod]
    public void TestFactory()
    {
        var initialLogger = InitialLogger.Create<LoggingInitialTest>();
        initialLogger.ShouldNotBeNull();
    }
}