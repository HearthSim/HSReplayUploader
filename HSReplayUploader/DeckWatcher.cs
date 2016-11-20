using System;
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
		private readonly SceneMode[] _modes;
		public List<Deck> Decks { get; private set; }
		public long SelectedDeckId { get; private set; }
		public SceneMode LastKnownMode { get; private set; }

		public Deck SelectedDeck => Decks?.FirstOrDefault(x => x.Id == SelectedDeckId);

		public DeckWatcher(SceneMode[] modes)
		{
			_modes = modes;
		}

		private bool _running;
		private bool _watch;

		public void Run()
		{
			_watch = true;
			if(!_running)
				Watch();
		}

		public void Stop()
		{
			_watch = false;
			Decks = null;
			SelectedDeckId = 0;
		}

		private async void Watch()
		{
			_running = true;
			while(_watch)
			{
				await Task.Delay(500);
				if(!_watch)
					break;
				Update();
			}
			_running = false;
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
				SelectedDeckId = id;
		}
	}
}
