using System.Collections.Generic;

namespace HSReplayUploader.LogReader.EventArgs
{
	internal class LogGameEndEventArgs : System.EventArgs
	{
		internal List<string> PowerLog { get; }

		public LogGameEndEventArgs(List<string> powerLog)
		{
			PowerLog = powerLog;
		}
	}
}