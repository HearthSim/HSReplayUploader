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
		public static async Task<UploadMetaData> Generate(Deck deck, int? hdtBuild)
		{
			var metaData = new UploadMetaData();

			var serverInfo = Reflection.GetServerInfo();
			if(serverInfo == null)
				Util.DebugLog?.WriteLine("UploadMetaDataGenerator: ServerInfo=null");
			while(serverInfo == null)
			{
				await Task.Delay(500);
				serverInfo = Reflection.GetServerInfo();
			}
			Util.DebugLog?.WriteLine("UploadMetaDataGenerator: Found ServerInfo");

			metaData.AuroraPassword = serverInfo.AuroraPassword;
			metaData.ServerIp = serverInfo.Address;
			metaData.ClientHandle = serverInfo.ClientHandle.ToString();
			metaData.GameHandle = serverInfo.GameHandle.ToString();
			metaData.SpectatePassword = serverInfo.SpectatorPassword;
			metaData.SpectatorMode = serverInfo.SpectatorMode;
			metaData.Resumable = serverInfo.Resumable;
			metaData.ServerPort = serverInfo.Port.ToString();
			metaData.ServerVersion = serverInfo.Version;

			var format = Reflection.GetFormat();
			metaData.Format = format;

			var gameType = Reflection.GetGameType();
			metaData.GameType = Util.GetBnetGameType(gameType, format);

			var matchInfo = Reflection.GetMatchInfo();
			metaData.FriendlyPlayerId = matchInfo.LocalPlayer.Id;
			metaData.LadderSeason = matchInfo.RankedSeasonId;
			metaData.ScenarioId = matchInfo.MissionId;

			var friendly = new UploadMetaData.Player()
			{
				DeckId = deck?.Id,
				DeckList = deck?.Cards.SelectMany(x => Enumerable.Repeat(x.Id, x.Count)).ToArray()
			};
			if(matchInfo.LocalPlayer.CardBackId > 0)
				friendly.Cardback = matchInfo.LocalPlayer.CardBackId;

			var opposing = new UploadMetaData.Player();
			if(matchInfo.OpposingPlayer.CardBackId > 0)
				opposing.Cardback = matchInfo.OpposingPlayer.CardBackId;

			metaData.Player1 = matchInfo.LocalPlayer.Id == 1 ? friendly : opposing;
			metaData.Player2 = matchInfo.LocalPlayer.Id == 2 ? friendly : opposing;

			metaData.HearthstoneBuild = hdtBuild;
			metaData.MatchStart = DateTime.Now.ToString("o");

			return metaData;
		}

	}
}
