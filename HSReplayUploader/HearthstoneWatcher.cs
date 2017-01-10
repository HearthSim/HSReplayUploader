using System;
using System.Linq;
using HSReplay;
using System.Threading.Tasks;
using HSReplayUploader.HearthstoneEnums;
using HSReplayUploader.LogReader;
using HSReplayUploader.LogReader.EventArgs;

namespace HSReplayUploader
{
	/// <summary>
	/// Hearthstone watcher class.
	/// Main entry point, contains events for tracking games starting and ending.
	/// </summary>
	public class HearthstoneWatcher
	{
		private readonly HsReplayClient _client;
		private readonly SceneMode[] _allowedModes;
		private readonly LogManager _logManager;
		private readonly DeckWatcher _deckWatcher;
		private readonly ProcWatcher _procWatcher;
		private UploadMetaData _metaData;
		private bool _running;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public delegate void GameEndEventHandler(object sender, GameEndEventArgs args);

		/// <summary>
		/// Called when the current game ends and the upload finished.
		/// EventArgs: UploadSuccessful will be false and Exception null,
		/// if the last known SceneMode does not match the allowedModes passed to the constructor.
		/// </summary>
		public event GameEndEventHandler OnGameEnd;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public delegate void GameStartEventHandler(object sender, GameStartEventArgs args);

		/// <summary>
		/// Called when a new game starts.
		/// </summary>
		public event GameStartEventHandler OnGameStart;

		/// <summary>
		/// </summary>
		/// <param name="client">HsReplay client</param>
		/// <param name="allowedModes">Array of SceneModes (i.e. game screen before the game starts), for which which the game should be uploaded.</param>
		/// <param name="hearthstoneDir">
		/// (Recommended) Hearthstone installation directory. 
		/// This will try to automatically find the directory if not provided.
		/// </param>
		public HearthstoneWatcher(HsReplayClient client, SceneMode[] allowedModes, string hearthstoneDir = null)
		{ 
			_client = client;
			_allowedModes = allowedModes;
			_logManager = new LogManager(hearthstoneDir);
			_deckWatcher = new DeckWatcher(allowedModes);
			_procWatcher = new ProcWatcher();
			Util.DebugLog?.WriteLine($"HearthstoneWatcher: HearthstoneDir={hearthstoneDir}, allowedModes={allowedModes.Select(x => x.ToString()).Aggregate((c, n) => c + ", " + n)}");
		}

		/// <summary>
		/// Starts the watcher.
		/// If hearthstoneDir was not provided in the ctor, this will not return until Hearthstone is running and the directory was detected.
		/// </summary>
		/// <returns></returns>
		public async Task Start()
		{
			Util.DebugLog?.WriteLine("HearthstoneWatcher.Start: Starting...");
			if(_running)
			{
				Util.DebugLog?.WriteLine("HearthstoneWatcher.Start: Watcher already running");
				return;
			}
			_running = true;
			_procWatcher.OnProcStopped += ProcWatcherOnProcStopped;
			_procWatcher.OnProcStarted += ProcWatcherOnProcStarted;
			_logManager.OnGameStart += HandleGameStart;
			_logManager.OnGameEnd += HandleGameEnd;
			_procWatcher.Run();
			_deckWatcher.Run();
			await _logManager.StartLogReader();
			Util.DebugLog?.WriteLine("HearthstoneWatcher.Start: Running.");
		}

		private void ProcWatcherOnProcStarted(object sender) => Util.DebugLog?.WriteLine("HearthstoneWatcher: Found proc");

		private async void ProcWatcherOnProcStopped(object sender)
		{
			Util.DebugLog?.WriteLine("HearthstoneWatcher: Hearthstone stopped. Restarting LogWatcher");
			_deckWatcher.Run();
			await _logManager.Stop();
			await _logManager.StartLogReader();
		}

		/// <summary>
		/// Stops the watcher.
		/// </summary>
		/// <returns></returns>
		public async Task Stop()
		{
			Util.DebugLog?.WriteLine("HearthstoneWatcher.Stop: Stopping...");
			if(!_running)
			{
				Util.DebugLog?.WriteLine("HearthstoneWatcher.Stop: Watcher is not running.");
				return;
			}
			_procWatcher.OnProcStopped -= ProcWatcherOnProcStopped;
			_procWatcher.OnProcStarted -= ProcWatcherOnProcStarted;
			_logManager.OnGameStart -= HandleGameStart;
			_logManager.OnGameEnd -= HandleGameEnd;
			_procWatcher.Stop();
			_deckWatcher.Stop();
			await _logManager.Stop();
			_running = false;
			Util.DebugLog?.WriteLine("HearthstoneWatcher.Stop: Stopped.");
		}

		private async void HandleGameStart(object sender, LogGameStartEventArgs args)
		{
			var deck = _deckWatcher.SelectedDeck;
			_deckWatcher.Stop();
			var build = Util.GetHearthstoneBuild(_logManager.HearthstoneDir);
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameStart: foundDeck={deck != null}, build={build}");
			_metaData = await UploadMetaDataGenerator.Generate(deck, build);
			OnGameStart?.Invoke(this, new GameStartEventArgs(_deckWatcher.LastKnownMode, _metaData.GameHandle));
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameStart: Game Started. LastKnownMode={_deckWatcher.LastKnownMode}, GameHandle={_metaData.GameHandle}");
		}

		private async void HandleGameEnd(object sender, LogGameEndEventArgs args)
		{
			Exception exception = null;
			string replayUrl = null;
			var uploadGame = _allowedModes.Contains(_deckWatcher.LastKnownMode);
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameEnd: Game ended. Uploading={uploadGame} (LastKnownMode={_deckWatcher.LastKnownMode})");
			if(uploadGame)
			{
				try
				{
					replayUrl = await _client.UploadLog(_metaData, args.PowerLog);
				}
				catch(Exception ex)
				{
					exception = ex;
				}
			}
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameEnd: Upload Successful={replayUrl != null}, Exception={exception}");
			OnGameEnd?.Invoke(this, new GameEndEventArgs(replayUrl != null, _metaData.GameHandle, exception));
			_deckWatcher.Run();
		}

		/// <summary>
		/// EventArgs for OnGameEnd
		/// </summary>
		public class GameEndEventArgs : EventArgs
		{
			/// <summary>
			/// Indicates whether the replay upload was successful.
			/// </summary>
			public bool UploadSuccessful { get; }

			/// <summary>
			/// More information in case the upload failed.
			/// </summary>
			public Exception Exception { get; }

			/// <summary>
			/// GameHandle of the started game.
			/// </summary>
			public string GameHandle { get; }

			internal GameEndEventArgs(bool success, string gameHandle, Exception exception = null)
			{
				UploadSuccessful = success;
				Exception = exception;
				GameHandle = gameHandle;
			}
		}

		/// <summary>
		/// EventArgs for OnGameStart
		/// </summary>
		public class GameStartEventArgs : EventArgs
		{
			/// <summary>
			/// Last known SceneMode (i.e. game screen) before the game started.
			/// </summary>
			public SceneMode Mode { get; }

			/// <summary>
			/// GameHandle of the started game.
			/// </summary>
			public string GameHandle { get; }

			internal GameStartEventArgs(SceneMode mode, string gameHandle)
			{
				Mode = mode;
				GameHandle = gameHandle;
			}
		}
	}
}
