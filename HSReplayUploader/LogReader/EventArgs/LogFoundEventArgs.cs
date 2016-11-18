namespace HSReplayUploader.LogReader.EventArgs
{
	internal class LogFoundEventArgs : System.EventArgs
	{
		public string Name { get; set; }

		public LogFoundEventArgs(string name)
		{
			Name = name;
		}
	}
}