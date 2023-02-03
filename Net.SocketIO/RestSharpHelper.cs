using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Reflection;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Collections;

using Nito.AsyncEx;

using RestSharp;
using RestSharp.Authenticators;

namespace Ecng.Net;

public static class RestSharpHelper
{
	public class UnexpectedResponseError : InvalidOperationException
	{
		public RestResponse Response { get; }

		public UnexpectedResponseError(RestResponse response) : base($"unexpected response code='{response.StatusCode}', msg='{response.ErrorMessage}', desc='{response.StatusDescription}', content='{response.Content}'")
			=> Response = response;
	}

	class AuthenticatorWrapper : IAuthenticator
	{
		private readonly SynchronizedDictionary<RestRequest, IAuthenticator> _authenticators = new();

		class Holder : Disposable
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

		ValueTask IAuthenticator.Authenticate(RestClient client, RestRequest request)
			=> _authenticators.TryGetValue(request, out var auth) && auth != null ? auth.Authenticate(client, request) : default;
	}

	private static readonly SynchronizedDictionary<object, RestClient> _clients = new();

	private static RestClient GetClient(object key)
	{
		return _clients.SafeAdd(key, _ =>
		{
			var asm = (key is string ? typeof(RestSharpHelper) : key.GetType()).Assembly;
			var prod = asm.GetAttribute<AssemblyProductAttribute>()?.Product;
			var ver = asm.GetName().Version;

			var options = new RestClientOptions
			{
				UserAgent = $"{prod}/{ver}",
			};

			return new RestClient(options) { Authenticator = new AuthenticatorWrapper() };
		});
	}

	public static void RemoveWhere(this ParametersCollection collection, Func<Parameter, bool> filter)
	{
		foreach (var par in collection.Where(filter).ToArray())
			collection.RemoveParameter(par);
	}

	public static void AddBodyAsStr(this RestRequest request, string bodyStr)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		request.AddStringBody(bodyStr, DataFormat.Json);
	}

	public static object Invoke(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null)
		=> request.Invoke<object>(url, caller, logVerbose, contentConverter);

	public static Task<object> InvokeAsync(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null)
		=> request.InvokeAsync<object>(url, caller, logVerbose, token, contentConverter);

	public static T Invoke<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null)
		=> AsyncContext.Run(() => request.InvokeAsync<T>(url, caller, logVerbose, CancellationToken.None, contentConverter));

	public static RestResponse<T> Invoke2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Func<string, string> contentConverter = null)
		=> AsyncContext.Run(() => request.InvokeAsync2<T>(url, caller, logVerbose, CancellationToken.None, contentConverter));

	public static async Task<T> InvokeAsync<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null)
		=> (await request.InvokeAsync2<T>(url, caller, logVerbose, token, contentConverter)).Data;

	public static async Task<RestResponse<T>> InvokeAsync2<T>(this RestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, CancellationToken token, Func<string, string> contentConverter = null, IAuthenticator auth = null)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		if (url is null)
			throw new ArgumentNullException(nameof(url));

		if (caller is null)
			throw new ArgumentNullException(nameof(caller));

		request.Resource = url.IsAbsoluteUri ? url.AbsoluteUri : url.OriginalString;

		logVerbose?.Invoke("Request {0}, '{1}' Args '{2}'.", new object[] { request.Method, url, request.Parameters.ToQueryString(false) });

		var client = GetClient(caller);
		using var _ = ((AuthenticatorWrapper)client.Authenticator!).RegisterRequest(request, auth);

		var response = await client.ExecuteAsync<object>(request, token);

		logVerbose?.Invoke("Response '{0}' (code {1}).", new object[] { response.Content, response.StatusCode });

		// https://restsharp.dev/usage/exceptions.html
		var networkFailure = response.ResponseStatus != ResponseStatus.Completed;
		if(networkFailure)
		{
			logVerbose?.Invoke("failed to complete reqeust: status={0}, msg={1}, err={2}", new object[] { response.ResponseStatus, response.ErrorMessage, response.ErrorException });
			throw new InvalidOperationException($"failed to complete request: {response.ResponseStatus}");
		}

		if (response.StatusCode != HttpStatusCode.OK || response.Content.IsEmpty())
			throw new UnexpectedResponseError(response);

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

	public static string ToQueryString(this IEnumerable<Parameter> parameters, bool encodeValue = true)
		=> parameters.CheckOnNull(nameof(parameters)).Select(p => $"{p.Name}={p.Value.Format(encodeValue)}").JoinAnd();
}
