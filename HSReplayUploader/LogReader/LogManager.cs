using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HSReplayUploader.LogReader.EventArgs;

namespace HSReplayUploader.LogReader
{
	internal enum GameState
	{
		Playing,
		InMenu
	}

	internal class LogManager
	{
		internal delegate void GameEndEventHandler(object sender, LogGameEndEventArgs args);
		internal event GameEndEventHandler OnGameEnd;

		internal delegate void GameStartEventHandler(object sStarter, LogGameStartEventArgs args);
		internal event GameStartEventHandler OnGameStart;

		public string HearthstoneDir { get; private set; }

		public bool FoundLog { get; private set; }

		private readonly List<string> _powerLog = new List<string>();
		private LogWatcher _logWatcher;
		private GameState _gameState = GameState.InMenu;

		public LogManager(string hearthstoneDir)
		{
			HearthstoneDir = hearthstoneDir;
		}

		public async Task Stop()
		{
			_logWatcher.OnNewLine -= OnLogWatcherOnOnNewLine;
			_logWatcher.OnLogFound -= LogWatcherOnOnLogFound;
			await _logWatcher.Stop();
		}

		internal async Task StartLogReader()
		{
			_powerLog.Clear();
			_gameState = GameState.InMenu;
			FoundLog = false;
			if(string.IsNullOrEmpty(HearthstoneDir))
				HearthstoneDir = await Util.GetHearthstoneDir();
			var readerInfo = new LogReaderInfo() {
				StartsWithFilters = new[] { "GameState." },
				FilePath = Path.Combine(HearthstoneDir, "Logs", "Power.log"),
				Name = "Power"
			};
			_logWatcher = new LogWatcher(readerInfo, 500);
			_logWatcher.OnNewLine += OnLogWatcherOnOnNewLine;
			_logWatcher.OnLogFound += LogWatcherOnOnLogFound;
			var entry = _logWatcher.FindEntryPoint("tag=GOLD_REWARD_STATE", "End Spectator");
			_logWatcher.Start(entry);
		}

		private void LogWatcherOnOnLogFound(object sender, LogFoundEventArgs args) => FoundLog = true;

		private void OnLogWatcherOnOnNewLine(object sender, LogLineEventArgs args)
		{
			var lines = args.Lines.Select(x => x.Line).ToList();
			var invokeEnd = false;
			foreach(var line in lines)
			{
				if(_gameState == GameState.InMenu && line.Contains("CREATE_GAME"))
				{
					_powerLog.Clear();
					OnGameStart?.Invoke(this, new LogGameStartEventArgs());
					_gameState = GameState.Playing;
				}
				else if(_gameState == GameState.Playing && line.Contains("tag=STATE value=COMPLETE"))
				{
					invokeEnd = true;
					_gameState = GameState.InMenu;
				}
			}
			_powerLog.AddRange(lines);
			if (invokeEnd)
				OnGameEnd?.Invoke(this, new LogGameEndEventArgs(_powerLog));
		}

	}
}
