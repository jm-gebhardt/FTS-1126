using Gebhardt.StoreWare.Agv.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
	/// <summary>
	/// Dummy class for logging mocks
	/// </summary>
	public class DummyColorConsoleTestClass
	{
		private readonly ILogger _logger;

		/// <summary>
		/// Constructor of dummy class
		/// </summary>
		/// <param name="logger"></param>
		public DummyColorConsoleTestClass(ILogger<DummyColorConsoleTestClass> logger)
		{
			_logger = logger;
		}
		

		/// <summary>
		/// Method to be called when mocking
		/// </summary>
		/// <param name="logLevel"></param>
		/// <param name="logEventId"></param>
		/// <param name="logMsg"></param>
		public void PleaseLogAnythingIMayGive(LogLevel logLevel, int logEventId, string logMsg)
		{
			// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
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

		private readonly TextLoggerConfiguration _textLoggerConfiguration = new();

		/// <summary>
		/// Create mock instances first
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			_loggerMock = new Mock<ILogger<DummyColorConsoleTestClass>>();
			_colorConsole = new DummyColorConsoleTestClass(_loggerMock.Object);
		}

		
		/// <summary>
		/// Injecting each logging provider
		/// </summary>
		[TestMethod]
		public void AddAllLoggingProvidersToMyHost_ThenGetTheServices()
		{
			var hostBuilder = Host.CreateDefaultBuilder()
				.ConfigureLogging(conf =>
				{
					// Add our custom logging providers - more than one possible
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
		/// Testing the log method only once
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
				if (_textLoggerConfiguration.LogLevels.ContainsKey(logLevel))
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

		/// <summary>
		/// Testing the method invocation when a non-null logger mock object is given
		/// </summary>
		[TestMethod]
		public void Test_Initialized_Logger_With_CallerAttributes()
		{
			// Arrange & Act
			if (_loggerMock != null)
			{
				LogWithCallerAttributes.Write(_loggerMock.Object, It.IsAny<string>());
			}

			// Assert
			_loggerMock?.Verify(l => l.Log(
				LogLevel.Information,
				0,
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				((Func<It.IsAnyType, Exception, string>)It.IsAny<object>())!), Times.Exactly(1));
		}

		/// <summary>
		/// Handling the case: logger mock object is null
		/// </summary>
		[TestMethod]
		public void Test_NullLogger_With_CallerAttributes()
		{
			// Arrange
			_loggerMock = null;

			// Act
			LogWithCallerAttributes.Write(_loggerMock?.Object, It.IsAny<string>());

			// Assert
			_loggerMock?.Verify(l => l.Log(
				LogLevel.Information,
				0,
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				((Func<It.IsAnyType, Exception, string>)It.IsAny<object>())!), Times.Never);
		}
	}
}
