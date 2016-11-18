using System;
using HSReplayUploader.LogConfig;

namespace HSReplayUploader
{
	/// <summary>
	/// Helepr class to verify the log.config file, required to generate replays, exists.
	/// </summary>
	public static class LogConfigHelper
	{
		/// <summary>
		/// Verifies the %LocalAppData%/Blizzard/Hearthstone/log.config file exists and contains the correct data to generate the Power.log
		/// and creates/updates it for the case it was not 
		/// </summary>
		/// <returns>The verification result: `LogConfigState` (Ok/Updated/Error) and `Exception` in case of error.</returns>
		public static LogConfigResult VerifyLogConfig()
		{
			try
			{
				var updated = LogConfigUpdater.CheckLogConfig();
				return new LogConfigResult(updated ? LogConfigState.Updated : LogConfigState.Ok);
			}
			catch(Exception ex)
			{
				return new LogConfigResult(LogConfigState.Error, ex);
			}
		}

		public enum LogConfigState
		{
			/// <summary>
			/// log.config is exists and correctly configured.
			/// </summary>
			Ok,
			/// <summary>
			/// log.config was created or updated. Hearthstone needs to be restarted in case it was running.
			/// </summary>
			Updated,
			/// <summary>
			/// Creating or updating the log.config failed. See LogConfigResult.Exception for more information.
			/// </summary>
			Error

		}

		/// <summary>
		/// Result of verifying/creating/updating the log.config file.
		/// In case of `State` being `LogConfigState.Error`, see `Exception` for more information.
		/// </summary>
		public class LogConfigResult
		{
			public LogConfigState State { get; }
			public Exception Exception { get; set; }

			internal LogConfigResult(LogConfigState state, Exception exception = null)
			{
				State = state;
				Exception = exception;
			}
		}
	}
}