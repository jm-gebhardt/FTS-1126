
namespace Gebhardt.StoreWare.Agv.Common.Logging
{
	/// <summary>
	/// Static class assembling every constant related to logging
	/// </summary>
	public static class LoggingConstants
	{
		/// <summary>
		/// Suffix of every written error file
		/// </summary>
		public const string ErrorFileSuffix = "_fehler.log";

		/// <summary>
		/// Suffix of every written non error file
		/// </summary>
		public const string NonErrorFileSuffix = "_meldung.log";

		/// <summary>
		/// Common DateTime format applied to every log message
		/// </summary>
		public const string LoggingDateFormat = "dd.MM.yyyy H:mm:ss.fff";

		/// <summary>
		/// Key to folder path in appsettings.json
		/// </summary>
		public const string SettingKeyFolderPath = "Logging:TextLogger:folderPath";

		/// <summary>
		/// Key to padding value in appsettings.json
		/// </summary>
		public const string SettingKeyPaddingRight = "Logging:TextLogger:paddingRight";

		/// <summary>
		/// Key to padding value in appsettings.json targeting the eventId
		/// </summary>
		public const string SettingKeyEventIdPaddingRight = "Logging:TextLogger:eventIdPaddingRight";
	}
}
