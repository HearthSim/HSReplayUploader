using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthMirror;
using HearthMirror.Objects;
using HSReplayUploader.HearthstoneEnums;

namespace HSReplayUploader
{
	internal class DeckWatcher
	{
		private readonly SceneMode[] _modes = { SceneMode.ADVENTURE, SceneMode.FRIENDLY, SceneMode.TAVERN_BRAWL, SceneMode.TOURNAMENT };
		public List<Deck> Decks { get; private set; }
		public long SelectedDeckId { get; private set; }
		public SceneMode LastKnownMode { get; private set; }

		public Deck SelectedDeck => Decks?.FirstOrDefault(x => x.Id == SelectedDeckId);

		private bool _running;
		private bool _watch;

		public void Run()
		{
			Util.DebugLog?.WriteLine("DeckWatcher.Run: Starting.");
			_watch = true;
			if(!_running)
			{
				SelectedDeckId = 0;
				Watch();
			}
		}

		public void Stop()
		{
			Util.DebugLog?.WriteLine("DeckWatcher.Watch: stopping.");
			_watch = false;
			Decks = null;
			SelectedDeckId = 0;
		}

		private async void Watch()
		{
			Util.DebugLog?.WriteLine("DeckWatcher.Watch: watching...");
			_running = true;
			while(_watch)
			{
				await Task.Delay(500);
				if(!_watch)
					break;
				Update();
			}
			_running = false;
			Util.DebugLog?.WriteLine("DeckWatcher.Watch: stopped watching.");
		}

		private void Update()
		{
			if(!Util.HearthstoneIsRunning)
				return;
			var scene = (int)Reflection.GetCurrentSceneMode();
			if(scene != (int)SceneMode.GAMEPLAY)
			{
				LastKnownMode = (SceneMode)scene;
				if(!_modes.Contains((SceneMode)scene))
				{
					Decks = null;
					return;
				}
			}
			if(Decks == null)
				Decks = Reflection.GetDecks();
			var id  = Reflection.GetSelectedDeckInMenu();
			if(id > 0)
			{
				if(id != SelectedDeckId)
					Util.DebugLog?.WriteLine($"DeckWatcher.Update: SelectedDeck={id}");
				SelectedDeckId = id;
			}
		}
	}
}
