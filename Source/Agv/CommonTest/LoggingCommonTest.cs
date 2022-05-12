using Gebhardt.StoreWare.Agv.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
	/// <summary />
	public class DummyColorConsoleTestClass
	{
		private readonly ILogger _logger;

		/// <summary />
		public DummyColorConsoleTestClass(ILogger<DummyColorConsoleTestClass> logger)
		{
			_logger = logger;
		}
		

		/// <summary />
		public void PleaseLogAnythingIMayGive(LogLevel logLevel, int logEventId, string logMsg)
		{
			_logger.Log(logLevel, logEventId, logMsg);
		}

		
	}

	/// <summary>
	/// Test class for common logging
	/// </summary>
	[TestClass]
	public class LoggingCommonTest
	{
		private Mock<ILogger<DummyColorConsoleTestClass>>? _loggerMock;
		private DummyColorConsoleTestClass? _colorConsole;

		private readonly ColorConsoleLoggerConfiguration _colorConsoleLoggerConfiguration = new();
		private readonly TextLoggerConfiguration _textLoggerConfiguration = new();

		/// <summary>
		/// 
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			_loggerMock = new Mock<ILogger<DummyColorConsoleTestClass>>();
			_colorConsole = new DummyColorConsoleTestClass(_loggerMock.Object);
		}

		
		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void AddAllLoggingProvidersToMyHost_ThenGetTheServices()
		{
			var hostBuilder = Host.CreateDefaultBuilder()
				.ConfigureLogging(conf =>
				{
					// Eradicate the default logging provider
					conf.ClearProviders();
					// Add our custom logging providers - more than one possible
					conf.AddColorConsoleLogger();
					conf.AddTextLogger();
				});

			var host = hostBuilder.Build();
			var logger = host.Services.GetRequiredService<ILogger<DummyColorConsoleTestClass>>();

			using (logger.BeginScope(123))
			{
				logger.LogInformation("Log info into console and text");
				logger.LogError("Log error into console and text...");
			}
		}
		

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		[DataRow(LogLevel.Information, 6546, "Log a piece of information")]
		[DataRow(LogLevel.Error, 666, "Log an error")]
		[DataRow(LogLevel.Warning, 4711, "Log a warning")]
		[DataRow(LogLevel.Critical, 666, "Log a critical point")]
		public void TestLogAnything_And_Test_If_Log_Method_Is_Called(LogLevel logLevel, int logEventId, string logMsg)
		{
			if (_colorConsole != null && _loggerMock != null)
			{
				if (_colorConsoleLoggerConfiguration.LogLevels.ContainsKey(logLevel) && _textLoggerConfiguration.LogLevels.ContainsKey(logLevel))
				{
					_colorConsole.PleaseLogAnythingIMayGive(logLevel, logEventId, logMsg);
					_loggerMock.Verify(l => l.Log(
						logLevel,
						It.IsAny<EventId>(),
						It.IsAny<It.IsAnyType>(),
						It.IsAny<Exception>(),
						((Func<It.IsAnyType, Exception, string>)It.IsAny<object>())!), Times.Exactly(1));
				}
				else
				{
					throw new IndexOutOfRangeException("Key not found in Dictionary");
				}
			}
			else
			{
				throw new NullReferenceException("Mock instance and colorConsole instance are null.");
			}
		}
	}
}
