namespace Ecng.Net;

using System.Reflection;
using System.Security;

using Nito.AsyncEx;

using RestSharp.Authenticators;

public static class RestSharpHelper
{
	private class AuthenticatorWrapper : IAuthenticator
	{
		private readonly SynchronizedDictionary<RestRequest, IAuthenticator> _authenticators = [];

		private class Holder : Disposable
		{
			private readonly RestRequest _request;
			private readonly AuthenticatorWrapper _parent;

			public Holder(AuthenticatorWrapper wrapper, RestRequest req, IAuthenticator auth)
			{
				_parent = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
				_request = req ?? throw new ArgumentNullException(nameof(req));
				_parent._authenticators.Add(_request, auth);
			}

			protected override void DisposeManaged()
			{
				_parent._authenticators.Remove(_request);
				base.DisposeManaged();
			}
		}

		public IDisposable RegisterRequest(RestRequest req, IAuthenticator authenticator) => new Holder(this, req, authenticator);

		ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
			=> _authenticators.TryGetValue(request, out var auth) && auth != null ? auth.Authenticate(client, request) : default;
	}

	private static readonly SynchronizedDictionary<object, RestClient> _clients = [];

	private static RestClient GetClient(object key)
	{
		return _clients.SafeAdd(key, _ =>
		{
			var asm = (key is string ? typeof(RestSharpHelper) : key.GetType()).Assembly;
			var prod = asm.GetAttribute<AssemblyProductAttribute>()?.Product;
			var ver = asm.GetName().Version;

			return new(new RestClientOptions
			{
				UserAgent = $"{prod}/{ver}",
				Authenticator = new AuthenticatorWrapper(),
			});
		});
	}

	public static void RemoveWhere(this RestRequest request, Func<Parameter, bool> filter)
	{
		foreach (var par in request.Parameters.Where(filter).ToArray())
			request.RemoveParameter(par);
	}

	public static void AddBodyAsStr(this RestRequest request, string bodyStr)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		request.AddStringBody(bodyStr, DataFormat.Json);
	}

	public static object Invoke(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> request.Invoke<object>(url, caller, logVerbose, contentConverter, throwIfEmptyResponse);

	public static Task<object> InvokeAsync(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> request.InvokeAsync<object>(url, caller, logVerbose, token, contentConverter, throwIfEmptyResponse);

	public static T Invoke<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> AsyncContext.Run(() => request.InvokeAsync<T>(url, caller, logVerbose, CancellationToken.None, contentConverter, throwIfEmptyResponse));

	public static RestResponse<T> Invoke2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> AsyncContext.Run(() => request.InvokeAsync2<T>(url, caller, logVerbose, CancellationToken.None, contentConverter, null, throwIfEmptyResponse));

	public static async Task<T> InvokeAsync<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> (await request.InvokeAsync2<T>(url, caller, logVerbose, token, contentConverter, null, throwIfEmptyResponse)).Data;

	public static Task<RestResponse<T>> InvokeAsync2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, IAuthenticator auth = null, bool throwIfEmptyResponse = true)
		=> InvokeAsync3<T>(request, url, caller, logVerbose, token, contentConverter, auth, throwIfEmptyResponse);

	public static async Task<RestResponse<T>> InvokeAsync3<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, IAuthenticator auth = null, bool throwIfEmptyResponse = true, Func<HttpStatusCode, bool> handleErrorStatus = null)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		if (url is null)
			throw new ArgumentNullException(nameof(url));

		if (caller is null)
			throw new ArgumentNullException(nameof(caller));

		request.Resource = url.IsAbsoluteUri ? url.AbsoluteUri : url.OriginalString;

		if (logVerbose is not null)
		{
			static string formatParams(IEnumerable<Parameter> parameters)
			{
				var arr = parameters.Where(p => p.Type != ParameterType.HttpHeader).ToArray();

				if (arr.Any(p => p.Type == ParameterType.RequestBody))
					return arr[0].Value.ToJson();

				return arr.Select(p => $"{p.Name}={p.Value}").JoinAnd();
			}

			static string formatHeaders(IEnumerable<Parameter> parameters)
			{
				var arr = parameters.Where(p => p.Type == ParameterType.HttpHeader).ToArray();
				return arr.Select(p => $"{p.Name}={p.Value}").JoinN();
			}

			logVerbose(@"Request ({0}): '{1}'
Headers:
{2}

Args:
{3}", [request.Method, url, formatHeaders(request.Parameters), formatParams(request.Parameters)]);
		}

		var client = GetClient(caller);
		using var _ = ((AuthenticatorWrapper)client.Options.Authenticator!).RegisterRequest(request, auth);

		var response = await client.ExecuteAsync<object>(request, token);

		if (logVerbose is not null)
			logVerbose("Response '{0}' (code {1}).", [response.Content, response.StatusCode]);

		// https://restsharp.dev/usage/exceptions.html
		if(response.ResponseStatus != ResponseStatus.Completed)
		{
			if (response.StatusCode == HttpStatusCode.NoContent)
			{
				if (throwIfEmptyResponse)
					throw response.ToError("Empty content.");

				return RestResponse<T>.FromResponse(response);
			}

			if (logVerbose is not null)
				logVerbose("failed to complete request: status={0}, msg={1}, err={2}", [response.ResponseStatus, response.ErrorMessage, response.ErrorException]);

			throw new InvalidOperationException($"failed to complete request (err={response.StatusCode}): {response.Content}");
		}

		if (response.StatusCode != HttpStatusCode.OK)
		{
			if (handleErrorStatus?.Invoke(response.StatusCode) != true)
				throw response.ToError();
		}
		else if (response.Content.IsEmpty())
		{
			if (throwIfEmptyResponse)
				throw response.ToError("Empty content.");

			return RestResponse<T>.FromResponse(response);
		}

		var result = RestResponse<T>.FromResponse(response);

		if (contentConverter != null)
		{
			result.Content = contentConverter(response.Content);

			if (result.Content.IsEmpty())
			{
				result.Content = default;
				result.Data = default;
				return result;
			}
		}

		result.Data = result.Content.DeserializeObject<T>();

		return result;
	}

	public static RestSharpException ToError(this RestResponse response, string message = default)
		=> new(message.IsEmpty($"unexpected response code='{response.StatusCode}', msg='{response.ErrorMessage}', desc='{response.StatusDescription}', content='{response.Content}'"), response);

	public static string ToQueryString(this IEnumerable<Parameter> parameters, bool encodeValue = true)
		=> parameters.CheckOnNull(nameof(parameters)).Select(p => $"{p.Name}={p.Value.Format(encodeValue)}").JoinAnd();

	public static IEnumerable<string> DecodeJWT(this string jwt)
	{
		if (jwt.IsEmpty())
			return [];

		static string decode(string base64Url)
		{
			var padded = base64Url.PadRight(base64Url.Length + (4 - base64Url.Length % 4) % 4, '=');
			var base64 = padded.Replace('-', '+').Replace('_', '/');
			return base64.Base64().UTF8();
		}

		var parts = jwt.SplitByDot();

		return parts.Select(decode);
	}

	public static RestRequest SetBearer(this RestRequest client, SecureString token)
		=> client.AddHeader("Authorization", AuthSchemas.Bearer.FormatAuth(token));
}