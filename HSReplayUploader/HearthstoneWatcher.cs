using System;
using System.Linq;
using HSReplay;
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
		private readonly LogManager _logManager = new LogManager();
		private readonly DeckWatcher _deckWatcher;
		private UploadMetaData _metaData;

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
			_logManager.OnGameStart += HandleGameStart;
			_logManager.OnGameEnd += HandleGameEnd;
			_logManager.StartLogReader(hearthstoneDir).Forget();
			_deckWatcher = new DeckWatcher(allowedModes);
			_deckWatcher.Run();
		}

		private async void HandleGameStart(object sender, LogGameStartEventArgs args)
		{
			var deck = _deckWatcher.SelectedDeck;
			_deckWatcher.Stop();
			var build = Util.GetHearthstoneBuild(_logManager.HearthstoneDir);
			_metaData = await UploadMetaDataGenerator.Generate(deck, build);
			OnGameStart?.Invoke(this, new GameStartEventArgs(_deckWatcher.LastKnownMode, _metaData.GameHandle));
		}

		private async void HandleGameEnd(object sender, LogGameEndEventArgs args)
		{
			Exception exception = null;
			string replayUrl = null;
			if(_allowedModes.Contains(_deckWatcher.LastKnownMode))
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
			OnGameEnd?.Invoke(this, new GameEndEventArgs(replayUrl != null, exception));
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

			internal GameEndEventArgs(bool success, Exception exception = null)
			{
				UploadSuccessful = success;
				Exception = exception;
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
