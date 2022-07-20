using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
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
			var initialLogger = Common.Logging.InitialLogger.Create<LoggingInitialTest>();
			Assert.IsNotNull(initialLogger);
		}
	}
}
