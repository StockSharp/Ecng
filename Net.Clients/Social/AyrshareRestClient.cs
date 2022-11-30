namespace Ecng.Net.Social;

public class AyrshareRestClient : RestBaseApiClient
{
	public enum AutoHashtagPositions
	{
		Auto,
		End,
	}

	public struct AutoHashtag
	{
		public int Max { get; set; }
		public AutoHashtagPositions Position { get; set; }
	}

	public struct PostResultItem
	{
		public string Status { get; set; }
		public string Id { get; set; }
		public string PostUrl { get; set; }
		public string Platform { get; set; }
		public string Code { get; set; }
		public string Message { get; set; }
	}

	public struct PostResult
	{
		public string Status { get; set; }
		public string Id { get; set; }
		public string Post { get; set; }
		public PostResultItem[] Errors { get; set; }
		public PostResultItem[] PostIds { get; set; }
	}

	public AyrshareRestClient(HttpMessageInvoker http, SecureString token)
        : base(http, CreateFormatter(), CreateFormatter())
    {
        BaseAddress = new("https://app.ayrshare.com/api/");
        AddAuth("Bearer", token.UnSecure());
    }

    private static MediaTypeFormatter CreateFormatter()
        => new JsonMediaTypeFormatter
        {
            SerializerSettings = {
                NullValueHandling = NullValueHandling.Ignore,
            }
        };

	// https://docs.ayrshare.com/rest-api/endpoints/post

	public Task<PostResult> PostAsync(
		string post,
		string[] platforms,
		string[] mediaUrls,
		bool? isVideo,
		DateTime? scheduleDate,
		bool? shortenLinks,
		bool? requiresApproval,
		AutoHashtag? autoHashtag,
		CancellationToken cancellationToken)
        =>	PostAsync<PostResult>(
				GetCurrentMethod(),
				cancellationToken,
				post,
				platforms,
				mediaUrls,
				isVideo,
				scheduleDate,
				shortenLinks,
				requiresApproval,
				autoHashtag
		);
}