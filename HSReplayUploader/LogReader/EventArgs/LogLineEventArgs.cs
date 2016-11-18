using System.Collections.Generic;

namespace HSReplayUploader.LogReader.EventArgs
{
	internal class LogLineEventArgs : System.EventArgs
	{
		public List<LogLineItem> Lines { get; set; }

		public LogLineEventArgs(List<LogLineItem> lines)
		{
			Lines = lines;
		}
	}
}