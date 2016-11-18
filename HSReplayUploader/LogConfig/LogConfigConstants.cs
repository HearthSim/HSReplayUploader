using System;
using System.IO;
using System.Text.RegularExpressions;

namespace HSReplayUploader.LogConfig
{
	internal class LogConfigConstants
	{
		public const string LogConfigFile = "log.config";
		public static readonly Regex NameRegex = new Regex(@"\[(?<value>(\w+))\]");
		public static readonly Regex LogLevelRegex = new Regex(@"LogLevel=(?<value>(\d+))");
		public static readonly Regex FilePrintingRegex = new Regex(@"FilePrinting=(?<value>(\w+))");
		public static readonly Regex ConsolePrintingRegex = new Regex(@"ConsolePrinting=(?<value>(\w+))");
		public static readonly Regex ScreenPrintingRegex = new Regex(@"ScreenPrinting=(?<value>(\w+))");
		public static readonly Regex VerboseRegex = new Regex(@"Verbose=(?<value>(\w+))");
		public static readonly string HearthstoneAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Blizzard\Hearthstone");
		public static readonly string LogConfigPath = Path.Combine(HearthstoneAppData, LogConfigFile);
		public static readonly LogConfigItem[] RequiredConfigItems = { new LogConfigItem("Power", false, true) };
		public static string[] Verbose => new[] { "Power" };
	}
}