using System;
using System.Collections.Generic;
using System.Linq;
using HSReplay;
using System.Threading.Tasks;
using HearthMirror;
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
		private const int UploadRetries = 2;
		private readonly HsReplayClient _client;
		private readonly BnetGameType[] _allowedModes;
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
		/// Called if GameMode matches one of the modes specified in the constructor,
		/// when the current game ends and the upload finished.
		/// </summary>
		public event GameEndEventHandler OnGameEnd;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public delegate void GameStartEventHandler(object sender, GameStartEventArgs args);

		/// <summary>
		/// Called if GameMode matches on of the modes specified in the constructor,
		/// when a new game starts.
		/// </summary>
		public event GameStartEventHandler OnGameStart;

		/// <summary>
		/// </summary>
		/// <param name="client">HsReplay client</param>
		/// <param name="allowedModes">Array of BnetGameTypes, for which which the game should be uploaded.</param>
		/// <param name="hearthstoneDir">
		/// (Recommended) Hearthstone installation directory. 
		/// This will try to automatically find the directory if not provided.
		/// </param>
		public HearthstoneWatcher(HsReplayClient client, BnetGameType[] allowedModes, string hearthstoneDir = null)
		{ 
			_client = client;
			_allowedModes = allowedModes.Concat(new [] {BnetGameType.BGT_UNKNOWN}).ToArray();
			_logManager = new LogManager(hearthstoneDir);
			_deckWatcher = new DeckWatcher();
			_procWatcher = new ProcWatcher();
			Util.DebugLog?.WriteLine($"HearthstoneWatcher: HearthstoneDir={hearthstoneDir}, allowedModes={allowedModes.Select(x => x.ToString()).Aggregate((c, n) => c + ", " + n)}");
		}

		/// <summary>
		/// Starts the watcher.
		/// If hearthstoneDir was not provided in the ctor, this will not return until Hearthstone is running and the directory was detected.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="HearthstoneInstallNotFoundException">
		/// Thrown if hearthstoenDir was not specified and automatically finding the install directory failed
		/// </exception>
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
			var accInfo = Reflection.GetAccountId();
			var invokeStart = _metaData.GameType.HasValue && _allowedModes.Contains((BnetGameType)_metaData.GameType);
			if(invokeStart)
				OnGameStart?.Invoke(this, new GameStartEventArgs((BnetGameType)(_metaData.GameType ?? 0), _metaData.GameHandle, accInfo?.Hi ?? 0, accInfo?.Lo ?? 0));
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameStart: Game Started. GameType={_metaData.GameType}, GameHandle={_metaData.GameHandle}, invokeStart={invokeStart}, accHi={accInfo?.Hi}, accLo={accInfo?.Lo}");
		}

		private async void HandleGameEnd(object sender, LogGameEndEventArgs args)
		{
			Exception exception = null;
			string replayUrl = null;
			var metaData = _metaData;
			var uploadGame = _allowedModes.Contains((BnetGameType)(metaData?.GameType ?? 0)) && !(metaData?.SpectatorMode ?? false);
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameEnd: Game ended. Uploading={uploadGame} GameType={metaData?.GameType} Spectator={metaData?.SpectatorMode}");

			if(uploadGame)
			{
				var trimmedLog = new List<string>();
				try
				{
					for(var i = args.PowerLog.Count - 1; i > 0; i--)
					{
						var line = args.PowerLog[i];
						trimmedLog.Add(line);
						if(line.Contains("CREATE_GAME"))
							break;
					}
					trimmedLog.Reverse();
				}
				catch(Exception)
				{
					trimmedLog = args.PowerLog;
				}

				for(var i = 0; i < UploadRetries; i++)
				{
					try
					{
						replayUrl = await _client.UploadLog(metaData, trimmedLog);
						break;
					}
					catch(Exception ex)
					{
						exception = ex;
					}

					var delay = 5 * (i + 1);
					Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameEnd: Upload try #{i + 1} failed. Retrying in {delay}s. Exception={exception}");
					await Task.Delay(1000 * delay);
				}
			}
			Util.DebugLog?.WriteLine($"HearthstoneWatcher.HandleGameEnd: Upload Successful={replayUrl != null}, url={replayUrl} Exception={exception}, invokeEnd={uploadGame}");
			if(uploadGame)
				OnGameEnd?.Invoke(this, new GameEndEventArgs(replayUrl != null, metaData?.GameHandle, exception));
			_deckWatcher.Run();
			_metaData = null;
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
			/// GameMode of current game
			/// </summary>
			public BnetGameType Mode { get; }

			/// <summary>
			/// GameHandle of the started game.
			/// </summary>
			public string GameHandle { get; }

			/// <summary>
			/// Account Hi value for the currently logged in Hearthstone account
			/// </summary>
			public ulong AccountHi { get; }

			/// <summary>
			/// Account Lo value for the currently logged in Hearthstone account
			/// </summary>
			public ulong AccountLo { get; }

			internal GameStartEventArgs(BnetGameType mode, string gameHandle, ulong accHi, ulong accLo)
			{
				Mode = mode;
				GameHandle = gameHandle;
				AccountHi = accHi;
				AccountLo = accLo;
			}
		}
	}
}
