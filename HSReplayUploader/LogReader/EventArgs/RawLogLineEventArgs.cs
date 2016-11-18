namespace HSReplayUploader.LogReader.EventArgs
{
	internal class RawLogLineEventArgs : System.EventArgs
	{
		public string Line { get; set; }

		public RawLogLineEventArgs(string line)
		{
			Line = line;
		}
	}
}