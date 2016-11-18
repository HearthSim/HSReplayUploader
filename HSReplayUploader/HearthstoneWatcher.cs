using System;
using HSReplay;
using HSReplayUploader.LogReader;
using HSReplayUploader.LogReader.EventArgs;

namespace HSReplayUploader
{
	public class HearthstoneWatcher
	{
		private readonly HsReplayClient _client;
		private readonly LogManager _logManager = new LogManager();
		private readonly DeckWatcher _deckWatcher = new DeckWatcher();
		private UploadMetaData _metaData;

		public delegate void GameEndEventHandler(object sender, GameEndEventArgs args);

		/// <summary>
		/// Called when the current game ends and the upload finished.
		/// </summary>
		public event GameEndEventHandler OnGameEnd;

		public delegate void GameStartEventHandler(object sender, EventArgs args);

		/// <summary>
		/// Called when a new game starts.
		/// </summary>
		public event GameStartEventHandler OnGameStart;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="client">HsReplay client</param>
		/// <param name="hearthstoneDir">
		/// (Recommended) Hearthstone installation directory. 
		/// This will try to automatically find the directory if not provided.
		/// </param>
		public HearthstoneWatcher(HsReplayClient client, string hearthstoneDir = null)
		{ 
			_client = client;
			_logManager.OnGameStart += HandleGameStart;
			_logManager.OnGameEnd += HandleGameEnd;
			_logManager.StartLogReader(hearthstoneDir).Forget();
			_deckWatcher.Run();
		}

		private async void HandleGameStart(object sender, LogGameStartEventArgs args)
		{
			var deck = _deckWatcher.SelectedDeck;
			_deckWatcher.Stop();
			var build = Util.GetHearthstoneBuild(_logManager.HearthstoneDir);
			_metaData = await UploadMetaDataGenerator.Generate(deck, build);
			OnGameStart?.Invoke(this, EventArgs.Empty);
		}

		private async void HandleGameEnd(object sender, LogGameEndEventArgs args)
		{
			Exception exception = null;
			try
			{
				await _client.UploadLog(_metaData, args.PowerLog);
			}
			catch(Exception ex)
			{
				exception = ex;
			}
			OnGameEnd?.Invoke(this, new GameEndEventArgs(exception));
			_deckWatcher.Run();
		}

		public class GameEndEventArgs : EventArgs
		{
			/// <summary>
			/// Uploading the replay was successful/failed.
			/// </summary>
			public bool UploadSuccessful => Exception == null;

			/// <summary>
			/// More information in case the upload failed.
			/// </summary>
			public Exception Exception { get; }

			internal GameEndEventArgs(Exception exception = null)
			{
				Exception = exception;
			}
		}
	}
}