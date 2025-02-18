namespace Ecng.Net;

using System.Security;

using Nito.AsyncEx;

using RestSharp.Authenticators;

/// <summary>
/// Provides helper methods to work with RestSharp requests and responses.
/// </summary>
public static class RestSharpHelper
{
	// Private class for wrapping an IAuthenticator.
	private class AuthenticatorWrapper : IAuthenticator
	{
		private readonly SynchronizedDictionary<RestRequest, IAuthenticator> _authenticators = new();

		private class Holder : Disposable
		{
			private readonly RestRequest _request;
			private readonly AuthenticatorWrapper _parent;

			/// <summary>
			/// Initializes a new instance of the <see cref="Holder"/> class and registers the authenticator.
			/// </summary>
			/// <param name="wrapper">The parent <see cref="AuthenticatorWrapper"/> instance.</param>
			/// <param name="req">The RestRequest to associate with the authenticator.</param>
			/// <param name="auth">The authenticator to register.</param>
			public Holder(AuthenticatorWrapper wrapper, RestRequest req, IAuthenticator auth)
			{
				_parent = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
				_request = req ?? throw new ArgumentNullException(nameof(req));
				_parent._authenticators.Add(_request, auth);
			}

			/// <summary>
			/// Releases the managed resources and unregisters the authenticator.
			/// </summary>
			protected override void DisposeManaged()
			{
				_parent._authenticators.Remove(_request);
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Registers an authenticator for a given request.
		/// </summary>
		/// <param name="req">The request to register the authenticator with.</param>
		/// <param name="authenticator">The authenticator to register.</param>
		/// <returns>A disposable that, when disposed, unregisters the authenticator.</returns>
		public IDisposable RegisterRequest(RestRequest req, IAuthenticator authenticator) => new Holder(this, req, authenticator);

		/// <inheritdoc/>
		ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
			=> _authenticators.TryGetValue(request, out var auth) && auth != null ? auth.Authenticate(client, request) : default;
	}

	private static readonly SynchronizedDictionary<object, RestClient> _clients = [];

	// Gets a RestClient instance based on a key.
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

	/// <summary>
	/// Removes parameters from the request that match the specified filter.
	/// </summary>
	/// <param name="request">The RestRequest to modify.</param>
	/// <param name="filter">The filter function to determine which parameters to remove.</param>
	public static void RemoveWhere(this RestRequest request, Func<Parameter, bool> filter)
	{
		foreach (var par in request.Parameters.Where(filter).ToArray())
			request.RemoveParameter(par);
	}

	/// <summary>
	/// Adds a string body to the request with JSON format.
	/// </summary>
	/// <param name="request">The RestRequest to which the body will be added.</param>
	/// <param name="bodyStr">The string representation of the body.</param>
	/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
	public static void AddBodyAsStr(this RestRequest request, string bodyStr)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		request.AddStringBody(bodyStr, DataFormat.Json);
	}

	/// <summary>
	/// Invokes the RestRequest synchronously.
	/// </summary>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>The deserialized object response.</returns>
	public static object Invoke(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> request.Invoke<object>(url, caller, logVerbose, contentConverter, throwIfEmptyResponse);

	/// <summary>
	/// Invokes the RestRequest asynchronously.
	/// </summary>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="token">The cancellation token.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>A task representing the asynchronous operation, returning the deserialized object response.</returns>
	public static Task<object> InvokeAsync(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> request.InvokeAsync<object>(url, caller, logVerbose, token, contentConverter, throwIfEmptyResponse);

	/// <summary>
	/// Invokes the RestRequest synchronously and returns a deserialized response of type T.
	/// </summary>
	/// <typeparam name="T">The expected type of the deserialized response.</typeparam>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>The deserialized response of type T.</returns>
	public static T Invoke<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> AsyncContext.Run(() => request.InvokeAsync<T>(url, caller, logVerbose, CancellationToken.None, contentConverter, throwIfEmptyResponse));

	/// <summary>
	/// Invokes the RestRequest synchronously and returns the full RestResponse of type T.
	/// </summary>
	/// <typeparam name="T">The expected type of the deserialized response.</typeparam>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>The full <see cref="RestResponse{T}"/> response.</returns>
	public static RestResponse<T> Invoke2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> AsyncContext.Run(() => request.InvokeAsync2<T>(url, caller, logVerbose, CancellationToken.None, contentConverter, null, throwIfEmptyResponse));

	/// <summary>
	/// Asynchronously invokes the RestRequest and returns a deserialized response of type T.
	/// </summary>
	/// <typeparam name="T">The expected type of the deserialized response.</typeparam>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="token">The cancellation token.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>A task representing the asynchronous operation, returning the deserialized response of type T.</returns>
	public static async Task<T> InvokeAsync<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, bool throwIfEmptyResponse = true)
		=> (await request.InvokeAsync2<T>(url, caller, logVerbose, token, contentConverter, null, throwIfEmptyResponse)).Data;

	/// <summary>
	/// Asynchronously invokes the RestRequest and returns the full <see cref="RestResponse{T}"/> response.
	/// </summary>
	/// <typeparam name="T">The expected type of the deserialized response.</typeparam>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="token">The cancellation token.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="auth">An optional authenticator for the request.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <returns>A task representing the asynchronous operation, returning the full <see cref="RestResponse{T}"/> response.</returns>
	public static Task<RestResponse<T>> InvokeAsync2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, IAuthenticator auth = null, bool throwIfEmptyResponse = true)
		=> InvokeAsync3<T>(request, url, caller, logVerbose, token, contentConverter, auth, throwIfEmptyResponse);

	/// <summary>
	/// Asynchronously invokes the RestRequest with extended error handling and returns the full <see cref="RestResponse{T}"/> response.
	/// </summary>
	/// <typeparam name="T">The expected type of the deserialized response.</typeparam>
	/// <param name="request">The RestRequest to invoke.</param>
	/// <param name="url">The URI endpoint for the request.</param>
	/// <param name="caller">The caller object for logging context.</param>
	/// <param name="logVerbose">An action to log verbose messages.</param>
	/// <param name="token">The cancellation token.</param>
	/// <param name="contentConverter">An optional function to convert the response content.</param>
	/// <param name="auth">An optional authenticator for the request.</param>
	/// <param name="throwIfEmptyResponse">Indicates whether to throw an exception if the response is empty.</param>
	/// <param name="handleErrorStatus">An optional function to handle error HTTP status codes.</param>
	/// <returns>A task representing the asynchronous operation, returning the full <see cref="RestResponse{T}"/> response.</returns>
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

	/// <summary>
	/// Converts a RestResponse into a RestSharpException with additional error details.
	/// </summary>
	/// <param name="response">The RestResponse instance.</param>
	/// <param name="message">An optional error message.</param>
	/// <returns>A new instance of <see cref="RestSharpException"/>.</returns>
	public static RestSharpException ToError(this RestResponse response, string message = default)
		=> new(message.IsEmpty($"unexpected response code='{response.StatusCode}', msg='{response.ErrorMessage}', desc='{response.StatusDescription}', content='{response.Content}'"), response);

	/// <summary>
	/// Converts a collection of parameters into a query string representation.
	/// </summary>
	/// <param name="parameters">The collection of parameters.</param>
	/// <param name="encodeValue">Indicates whether the parameter values should be encoded.</param>
	/// <returns>A query string representing the parameters.</returns>
	public static string ToQueryString(this IEnumerable<Parameter> parameters, bool encodeValue = true)
		=> parameters.CheckOnNull(nameof(parameters)).Select(p => $"{p.Name}={p.Value.Format(encodeValue)}").JoinAnd();

	/// <summary>
	/// Decodes a JSON Web Token (JWT) into its component parts.
	/// </summary>
	/// <param name="jwt">The JWT string.</param>
	/// <returns>An enumerable of strings representing the decoded parts of the JWT.</returns>
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

	/// <summary>
	/// Adds a Bearer authorization header to the RestRequest.
	/// </summary>
	/// <param name="client">The RestRequest to modify.</param>
	/// <param name="token">The secure token used for Bearer authentication.</param>
	/// <returns>The modified RestRequest.</returns>
	public static RestRequest SetBearer(this RestRequest client, SecureString token)
		=> client.AddHeader(HttpHeaders.Authorization, AuthSchemas.Bearer.FormatAuth(token));
}