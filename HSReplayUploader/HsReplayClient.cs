using System.Collections.Generic;
using System.Threading.Tasks;
using HSReplay;

namespace HSReplayUploader
{
	public class HsReplayClient
	{
		private readonly bool _testData;
		public string UploadToken { get; private set; }
		private readonly HSReplay.HsReplayClient _client;

		/// <summary>
		///  Creates a new HSReplayClient instance.
		/// </summary>
		/// <param name="apiKey">HsReplay API key</param>
		/// <param name="testData">Sets "test_data" flag on any HSReplay uploads/creations. Set this when sending "garbage" data.</param>
		public HsReplayClient(string apiKey, bool testData = false)
		{
			_testData = testData;
			_client = new HSReplay.HsReplayClient(apiKey, "HSReplayUploader/1.1.4", testData);
		}

		/// <summary>
		/// Creates a new HSReplayClient instance.
		/// </summary>
		/// <param name="apiKey">HsReplay API key</param>
		/// <param name="uploadToken">Existing user token previously obtained from CreateUploadToken()</param>
		/// <param name="testData">Sets "test_data" flag on any HSReplay.net uploads/creations. Set this when sending "garbage" data.</param>
		public HsReplayClient(string apiKey, string uploadToken, bool testData = false) : this(apiKey, testData)
		{
			UploadToken = uploadToken;
		}
		
		/// <summary>
		/// Creates a new HSReplay.net user token. 
		/// Store this token and pass it to the constructor in the future.
		/// </summary>
		/// <returns>User token generated by HSReplay.net</returns>
		public async Task<string> CreateUploadToken()
		{
			var token = await _client.CreateUploadToken();
			UploadToken = token;
			return token;
		}

		internal async Task<string> UploadLog(UploadMetaData metaData, List<string> log)
		{
			if(_testData)
				metaData.TestData = true;
			var request = await _client.CreateUploadRequest(metaData, UploadToken);
			await _client.UploadLog(request, log);
			return request.ReplayUrl;
		}

		/// <summary>
		/// Returns a url to claim the user account.
		/// This requires an existing upload token!
		/// </summary>
		/// <returns></returns>
		public async Task<string> GetClaimAccountUrl() => await _client.GetClaimAccountUrl(UploadToken);

		/// <summary>
		/// Returns the BattleTag of the user if the account was claimed, null otherwise.
		/// </summary>
		/// <returns></returns>
		public async Task<string> GetLinkedBattleTag() => (await _client.GetAccountStatus(UploadToken))?.User?.Username;
	}
}
