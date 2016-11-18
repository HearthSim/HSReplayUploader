using System.Collections.Generic;

namespace HSReplayUploader.LogConfig
{
	internal class LogConfig
	{
		public bool Updated { get; private set; }

		public List<LogConfigItem> Items { get; } = new List<LogConfigItem>();

		public void Add(LogConfigItem configItem)
		{
			Items.Add(configItem);
			Updated = true;
		}

		public void Verify()
		{
			foreach(var item in Items)
				Updated |= item.VerifyAndUpdate();
		}
	}
}