using System;

namespace HSReplayUploader.Exceptions
{
	public class HearthstoneInstallNotFoundException : Exception
	{
		public HearthstoneInstallNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
