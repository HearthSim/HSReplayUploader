using System.Threading.Tasks;

namespace HSReplayUploader
{
	internal class ProcWatcher
	{
		private bool _procRunning;
		private bool _running;
		private bool _watch;

		internal delegate void ProcStartedEventHandler(object sender);
		internal delegate void ProcStoppedEventHandler(object sender);
		internal event ProcStartedEventHandler OnProcStarted;
		internal event ProcStoppedEventHandler OnProcStopped;

		public void Run()
		{
			_watch = true;
			if(!_running)
				Watch();
		}

		public void Stop()
		{
			_watch = false;
			_procRunning = false;
		}

		public async void Watch()
		{
			_running = true;
			while(_watch)
			{
				if(!_watch)
					break;
				var procRunning = Util.HearthstoneIsRunning;
				if(procRunning != _procRunning)
				{
					_procRunning = procRunning;
					if(procRunning)
						OnProcStarted?.Invoke(this);
					else
						OnProcStopped?.Invoke(this);
				}
				await Task.Delay(1000);

			}
			_running = false;
		}
	}
}
