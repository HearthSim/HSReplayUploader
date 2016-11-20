using System;
using HSReplayUploader;
using HSReplayUploader.HearthstoneEnums;

namespace HSReplayUploaderDemo
{
	internal class Program
	{
		const string MyApiKey = "YOUR_API_KEY";

		private static void Main(string[] args)
		{
			//1. Ensure Hearthstone is generating the Power.log file
			var logConfigState = LogConfigHelper.VerifyLogConfig();
			switch(logConfigState.State)
			{
				case LogConfigHelper.LogConfigState.Ok:
					Console.WriteLine("log.config already set up");
					break;
				case LogConfigHelper.LogConfigState.Updated:
					Console.WriteLine("log.config was updated/created. Hearthstone might need to be restarted.");
				break;
				case LogConfigHelper.LogConfigState.Error:
					Console.WriteLine("Was not able to update/create log.config.");
					Console.WriteLine(logConfigState.Exception);
				break;
			}

			//2a. Create new HsReplayClient instance and request/store new user token
			var client = new HsReplayClient(MyApiKey, testData: true);
			var token = client.CreateUploadToken().Result;
			//MyConfig["HSReplayUserToken"] = token;

			//2b. Create new HsReplayClient, passing the existing user token
			//var client = new HSReplay(MyApiKey, MyConfig[HSReplayUserToken]);

			//3. Claim account process [NOT FINAL VERSION]
			var claimUrl = client.GetClaimAccountUrl().Result;
			Console.WriteLine($"Visit [https://hsreplay.net/{claimUrl}] to claim the account");

			//4. Create new HearthstoneWatcher instance and hook onto desired events.
			string hearthstoneDir = null; //MyConfig[HearthstoneInstallDir]
			var watcher = new HearthstoneWatcher(client, new[] {SceneMode.FRIENDLY}, hearthstoneDir);
			watcher.OnGameStart += (sender, eventArgs) => Console.WriteLine($"A new game started! LastKnownScene={eventArgs.Mode}");
			watcher.OnGameEnd += (sender, eventArgs) => Console.WriteLine($"Game ended! UploadSuccessful={eventArgs.UploadSuccessful}, Exception={eventArgs.Exception}");

			while(true)
				Console.ReadKey();
		}
	}
}
