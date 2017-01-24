using System;
using System.Linq;
using System.Threading.Tasks;
using HearthMirror;
using HearthMirror.Objects;
using HSReplay;

namespace HSReplayUploader
{
	internal class UploadMetaDataGenerator
	{
		private const int MaxReflectionTries = 5;

		public static async Task<UploadMetaData> Generate(Deck deck, int? hearthstoneBuild)
		{
			var metaData = new UploadMetaData();

			var serverInfoResult = await TryGetReflection(Reflection.GetServerInfo);
			if(serverInfoResult.Successful)
			{
				var serverInfo = serverInfoResult.Result;
				metaData.AuroraPassword = serverInfo.AuroraPassword;
				metaData.ServerIp = serverInfo.Address;
				metaData.ClientHandle = serverInfo.ClientHandle.ToString();
				metaData.GameHandle = serverInfo.GameHandle.ToString();
				metaData.SpectatePassword = serverInfo.SpectatorPassword;
				metaData.SpectatorMode = serverInfo.SpectatorMode;
				metaData.Resumable = serverInfo.Resumable;
				metaData.ServerPort = serverInfo.Port.ToString();
				metaData.ServerVersion = serverInfo.Version;
			}

			var formatResult = await TryGetReflection(Reflection.GetFormat);
			if(formatResult.Successful)
			{
				metaData.Format = formatResult.Result;
				var gameTypeResult = await TryGetReflection(Reflection.GetGameType);
				if(gameTypeResult.Successful)
					metaData.GameType = Util.GetBnetGameType(gameTypeResult.Result, formatResult.Result);
			}


			var friendly = new UploadMetaData.Player()
			{
				DeckId = deck?.Id,
				DeckList = deck?.Cards?.Where(x => x != null).SelectMany(x => Enumerable.Repeat(x.Id, x.Count)).ToArray()
			};
			var opposing = new UploadMetaData.Player();

			var matchInfoResult = await TryGetReflection(Reflection.GetMatchInfo);
			if(matchInfoResult.Successful)
			{
				var matchInfo = matchInfoResult.Result;
				metaData.FriendlyPlayerId = matchInfo.LocalPlayer.Id;
				metaData.LadderSeason = matchInfo.RankedSeasonId;
				metaData.ScenarioId = matchInfo.MissionId;

				if(matchInfo.LocalPlayer.CardBackId > 0)
					friendly.Cardback = matchInfo.LocalPlayer.CardBackId;

				if(matchInfo.OpposingPlayer.CardBackId > 0)
					opposing.Cardback = matchInfo.OpposingPlayer.CardBackId;
					
				metaData.Player1 = matchInfo.LocalPlayer.Id == 1 ? friendly : opposing;
				metaData.Player2 = matchInfo.LocalPlayer.Id == 2 ? friendly : opposing;
			}


			metaData.HearthstoneBuild = hearthstoneBuild;
			metaData.MatchStart = DateTime.Now.ToString("o");

			return metaData;
		}

		private static async Task<ReflectionResult<T>> TryGetReflection<T>(Func<T> action)
		{
			var value = action.Invoke();
			if(value != null)
				return new ReflectionResult<T>(value);
			Util.DebugLog?.WriteLine($"UploadMetaDataGenerator: {action.Method.Name} is null");
			for(var i = 0; i < MaxReflectionTries; i++)
			{
				await Task.Delay(500);
				if((value = action.Invoke()) != null)
				{
					Util.DebugLog?.WriteLine($"UploadMetaDataGenerator: found {action.Method.Name}");
					return new ReflectionResult<T>(value);
				}
			}
			return new ReflectionResult<T>(default(T));
		}

		private class ReflectionResult<T>
		{
			public ReflectionResult(T result)
			{
				Result = result;
			}

			public T Result { get; }
			public bool Successful => !Result?.Equals(default(T)) ?? false;
		}
	}

}
