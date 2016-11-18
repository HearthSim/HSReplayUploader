using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthMirror;
using HearthMirror.Enums;
using HearthMirror.Objects;

namespace HSReplayUploader
{
	internal class DeckWatcher
	{
		public List<Deck> Decks { get; private set; }
		public long SelectedDeckId { get; private set; }

		public Deck SelectedDeck => Decks?.FirstOrDefault(x => x.Id == SelectedDeckId);

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
			var scene = Reflection.GetCurrentSceneMode();
			if(scene != SceneMode.FRIENDLY && scene != SceneMode.GAMEPLAY)
			{
				Decks = null;
				return;
			}
			if(Decks == null)
				Decks = Reflection.GetDecks();
			var id  = Reflection.GetSelectedDeckInMenu();
			if(id > 0)
				SelectedDeckId = id;
		}
	}
}