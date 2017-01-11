using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HSReplayUploader.HearthstoneEnums;
using static HSReplayUploader.HearthstoneEnums.BnetGameType;

namespace HSReplayUploader
{
	public static class Util
	{
		public static ILog DebugLog { get; set; }

		private static Process GetHearthstoneProc()
		{
			try
			{
				return Process.GetProcessesByName("Hearthstone").FirstOrDefault();
			}
			catch(Exception)
			{
				return null;
			}
		}

		internal static bool HearthstoneIsRunning => GetHearthstoneProc() != null;

		internal static async Task<string> GetHearthstoneDir()
		{
			Process proc;
			while((proc = GetHearthstoneProc()) == null)
				await Task.Delay(500);
			var dir = new FileInfo(proc.MainModule.FileName).Directory?.FullName;
			return dir;
		}

		internal static int? GetHearthstoneBuild(string installDir)
		{
			if(string.IsNullOrEmpty(installDir))
				return null;
			var exe = Path.Combine(installDir, "Hearthstone.exe");
			return !File.Exists(exe) ? (int?)null : FileVersionInfo.GetVersionInfo(exe).FilePrivatePart;
		}

		internal static int GetBnetGameType(int gameType, int format) => (int)GetBnetGameType((GameType)gameType, (FormatType)format);

		private static BnetGameType GetBnetGameType(GameType gameType, FormatType format)
		{
			switch(gameType)
			{
				case GameType.GT_UNKNOWN:
					return BGT_UNKNOWN;
				case GameType.GT_VS_AI:
					return BGT_VS_AI;
				case GameType.GT_VS_FRIEND:
					return BGT_FRIENDS;
				case GameType.GT_TUTORIAL:
					return BGT_TUTORIAL;
				case GameType.GT_ARENA:
					return BGT_ARENA;
				case GameType.GT_TEST:
					return BGT_TEST1;
				case GameType.GT_RANKED:
					return format == FormatType.FT_STANDARD ? BGT_RANKED_STANDARD : BGT_RANKED_WILD;
				case GameType.GT_CASUAL:
					return format == FormatType.FT_STANDARD ? BGT_CASUAL_STANDARD : BGT_CASUAL_WILD;
				case GameType.GT_TAVERNBRAWL:
					return BGT_TAVERNBRAWL_PVP;
				case GameType.GT_TB_1P_VS_AI:
					return BGT_TAVERNBRAWL_1P_VERSUS_AI;
				case GameType.GT_TB_2P_COOP:
					return BGT_TAVERNBRAWL_2P_COOP;
				case GameType.GT_LAST:
					return BGT_LAST;
				default:
					return BGT_UNKNOWN;
			}
		}
	}

	internal static class TaskExtensions
	{
		internal static void Forget(this Task task)
		{
		}
	}
}
