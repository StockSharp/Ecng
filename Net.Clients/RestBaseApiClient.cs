namespace Ecng.Net;

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;

using Ecng.Reflection;

/// <summary>
/// Abstract base class for creating REST API clients.
/// Provides functionality to execute HTTP requests with retry and caching support.
/// </summary>
public abstract class RestBaseApiClient(HttpMessageInvoker http, MediaTypeFormatter request, MediaTypeFormatter response)
{
	private static readonly SynchronizedDictionary<(Type type, string methodName), MethodInfo> _methodsCache = [];

	/// <summary>
	/// Gets or sets the base address for the API.
	/// </summary>
	protected Uri BaseAddress { get; set; }

	/// <summary>
	/// Gets the underlying HTTP message invoker used for sending requests.
	/// </summary>
	protected HttpMessageInvoker Http { get; } = http ?? throw new ArgumentNullException(nameof(http));

	/// <summary>
	/// Gets the media type formatter used for request serialization.
	/// </summary>
	protected MediaTypeFormatter RequestFormatter { get; } = request ?? throw new ArgumentNullException(nameof(request));

	/// <summary>
	/// Gets the media type formatter used for response deserialization.
	/// </summary>
	protected MediaTypeFormatter ResponseFormatter { get; } = response ?? throw new ArgumentNullException(nameof(response));

	/// <summary>
	/// Gets the collection of headers to add per request.
	/// </summary>
	public IDictionary<string, string> PerRequestHeaders { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Gets or sets the cache used for storing REST API client responses.
	/// </summary>
	public IRestApiClientCache Cache { get; set; }

	/// <summary>
	/// Occurs when a request is about to be executed.
	/// </summary>
	public event Action<HttpMethod, Uri, object> LogRequest;

	/// <summary>
	/// Gets or sets a value indicating whether detailed error information should be extracted from non-success responses.
	/// </summary>
	public bool ExtractBadResponse { get; set; }

	private RetryPolicyInfo _retryPolicy = new();

	/// <summary>
	/// Gets the retry policy configuration for API calls.
	/// </summary>
	public RetryPolicyInfo RetryPolicy
	{
		get => _retryPolicy;
		set => _retryPolicy = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Gets a value indicating whether a single argument should be sent as a plain value.
	/// </summary>
	protected virtual bool PlainSingleArg => true;

	/// <summary>
	/// Gets a value indicating whether an exception should be thrown for non-success status codes.
	/// </summary>
	protected virtual bool ThrowIfNonSuccessStatusCode => true;

	/// <summary>
	/// Adds an authentication header using the specified authentication scheme and value.
	/// </summary>
	/// <param name="schema">The authentication scheme.</param>
	/// <param name="value">The authentication value.</param>
	protected void AddAuth(AuthenticationSchemes schema, string value)
		=> AddAuth(schema.ToString(), value);

	/// <summary>
	/// Adds a Bearer authentication header with the specified token.
	/// </summary>
	/// <param name="token">The Bearer token.</param>
	protected void AddAuthBearer(string token)
		=> AddAuth(AuthSchemas.Bearer, token);

	/// <summary>
	/// Adds an authentication header with the specified schema and value.
	/// </summary>
	/// <param name="schema">The authentication scheme.</param>
	/// <param name="value">The authentication value.</param>
	protected void AddAuth(string schema, string value)
		=> PerRequestHeaders.Add(HttpHeaders.Authorization, schema.FormatAuth(value));

	/// <summary>
	/// Formats the request parameters.
	/// </summary>
	/// <param name="parameters">A dictionary of parameter names and values.</param>
	/// <returns>The formatted request object.</returns>
	protected virtual object FormatRequest(IDictionary<string, object> parameters)
		=> parameters;

	/// <summary>
	/// Gets or sets a value indicating whether API calls should be traced.
	/// </summary>
	public bool Tracing { get; set; }

	/// <summary>
	/// Traces the API call by writing the HTTP method, URI, and elapsed time.
	/// </summary>
	/// <param name="method">The HTTP method used.</param>
	/// <param name="uri">The request URI.</param>
	/// <param name="elapsed">The elapsed time for the call.</param>
	protected virtual void TraceCall(HttpMethod method, Uri uri, TimeSpan elapsed)
	{
		Trace.WriteLine($"{method} {uri}: {elapsed}");
	}

	/// <summary>
	/// Validates the HTTP response.
	/// Throws an exception if the response is not successful and <see cref="ThrowIfNonSuccessStatusCode"/> is true.
	/// </summary>
	/// <param name="response">The HTTP response message.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	protected virtual async Task ValidateResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response is null)
			throw new ArgumentNullException(nameof(response));

		if (!ThrowIfNonSuccessStatusCode || response.IsSuccessStatusCode)
			return;

		if (ExtractBadResponse)
		{
			var errorText = await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
						cancellationToken
#endif
			);

#if NET5_0_OR_GREATER
			throw new HttpRequestException($"{response.StatusCode} ({(int)response.StatusCode}): {errorText}", null, response.StatusCode);
#else
			throw new HttpRequestException($"{response.StatusCode} ({(int)response.StatusCode}): {errorText}");
#endif
		}

		response.EnsureSuccessStatusCode();
	}

	/// <summary>
	/// Retrieves the result from the HTTP response content.
	/// </summary>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="response">The HTTP response message.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	protected virtual Task<TResult> GetResultAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		return typeof(TResult) == typeof(VoidType)
			? default(TResult).FromResult()
			: response.Content.ReadAsAsync<TResult>([ResponseFormatter], cancellationToken);
	}

	/// <summary>
	/// Creates an <see cref="HttpRequestMessage"/> for the specified HTTP method and URI.
	/// Adds the accepted media type and per-request headers.
	/// </summary>
	/// <param name="method">The HTTP method.</param>
	/// <param name="uri">The request URI.</param>
	/// <returns>The constructed <see cref="HttpRequestMessage"/>.</returns>
	protected HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
	{
		var request = new HttpRequestMessage(method, uri);

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(RequestFormatter.SupportedMediaTypes.First().MediaType));

		if (PerRequestHeaders.Count > 0)
		{
			foreach (var pair in PerRequestHeaders)
				request.Headers.Add(pair.Key, pair.Value);
		}

		return request;
	}

	/// <summary>
	/// Constructs an HTTP request message with the specified method, URI, and optional body.
	/// </summary>
	/// <param name="method">The HTTP method to use.</param>
	/// <param name="uri">The request URI.</param>
	/// <param name="body">The request body.</param>
	/// <returns>The <see cref="HttpRequestMessage"/> ready to be sent.</returns>
	protected virtual HttpRequestMessage GetRequest(HttpMethod method, Uri uri, object body)
	{
		var request = CreateRequest(method, uri);

		if (body is not null)
			request.Content = new ObjectContent<object>(body, RequestFormatter);

		return request;
	}

	/// <summary>
	/// Executes an API call using the specified HTTP method, URI, and request body.
	/// Applies caching and retries as configured.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="method">The HTTP method to use.</param>
	/// <param name="uri">The request URI.</param>
	/// <param name="body">The request body.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, CancellationToken cancellationToken)
	{
		var cache = Cache;

		if (cache != null && cache.TryGet<TResult>(method, uri, body, out var cached))
			return cached;

		LogRequest?.Invoke(method, uri, body);

		using var request = GetRequest(method, uri, body);

		var watch = Tracing ? Stopwatch.StartNew() : null;

		var result = await DoAsync<TResult>(request, cancellationToken);

		if (watch is not null)
		{
			watch.Stop();
			TraceCall(method, uri, watch.Elapsed);
		}

		cache?.Set(method, uri, body, result);
		return result;
	}

	/// <summary>
	/// Sends the specified HTTP request message and processes the response.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="request">The HTTP request message.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected async Task<TResult> DoAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using var response = await Http.SendAsync(request, cancellationToken);

		await ValidateResponseAsync(response, cancellationToken);

		return await GetResultAsync<TResult>(response, cancellationToken);
	}

	/// <summary>
	/// Executes a POST request for the specified method name and arguments.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="methodName">The name of the method corresponding to the API endpoint.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <param name="args">The arguments for the API method.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected Task<TResult> PostAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
		=> RetryPolicy.TryRepeat(t =>
		{
			var method = HttpMethod.Post;
			var (url, parameters, callerMethod) = GetInfo(method, methodName, args);

			object body;

			if (parameters.Length > 1 || !PlainSingleArg)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (name, value) in parameters)
				{
					if (value is not null)
						dict.Add(name, value);
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value, callerMethod, method);

			return DoAsync<TResult>(method, url, body, t);
		}, RetryPolicy.WriteMaxCount, cancellationToken);

	/// <summary>
	/// Executes a GET request for the specified method name and arguments.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="methodName">The name of the method corresponding to the API endpoint.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <param name="args">The arguments for the API method.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected Task<TResult> GetAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
		=> RetryPolicy.TryRepeat(t =>
		{
			var method = HttpMethod.Get;
			var (url, parameters, _) = GetInfo(method, methodName, args);

			if (parameters.Length > 0)
			{
				foreach (var (name, value) in parameters)
				{
					if (value is not null)
						url.QueryString.Append(name, value?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(method, url, null, t);
		}, RetryPolicy.ReadMaxCount, cancellationToken);

	/// <summary>
	/// Executes a DELETE request for the specified method name and arguments.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="methodName">The name of the method corresponding to the API endpoint.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <param name="args">The arguments for the API method.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected Task<TResult> DeleteAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
		=> RetryPolicy.TryRepeat(t =>
		{
			var method = HttpMethod.Delete;
			var (url, parameters, _) = GetInfo(method, methodName, args);

			if (parameters.Length > 0)
			{
				foreach (var (name, value) in parameters)
				{
					if (value is not null)
						url.QueryString.Append(name, value?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(method, url, null, t);
		}, RetryPolicy.WriteMaxCount, cancellationToken);

	/// <summary>
	/// Executes a PUT request for the specified method name and arguments.
	/// </summary>
	/// <typeparam name="TResult">The type of the result expected.</typeparam>
	/// <param name="methodName">The name of the method corresponding to the API endpoint.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <param name="args">The arguments for the API method.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	protected Task<TResult> PutAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
		=> RetryPolicy.TryRepeat(t =>
		{
			var method = HttpMethod.Put;
			var (url, parameters, callerMethod) = GetInfo(method, methodName, args);

			object body;

			if (parameters.Length > 1 || !PlainSingleArg)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (name, value) in parameters)
				{
					if (value is not null)
						dict.Add(name, value);
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value, callerMethod, method);

			return DoAsync<TResult>(method, url, body, t);
		}, RetryPolicy.WriteMaxCount, cancellationToken);

	/// <summary>
	/// Gets the current method name.
	/// Intended for use with caller information.
	/// </summary>
	/// <param name="methodName">The caller method name automatically provided.</param>
	/// <returns>The current method name.</returns>
	protected static string GetCurrentMethod([CallerMemberName]string methodName = "")
		=> methodName;

	/// <summary>
	/// Formats the request URI by removing "Async" suffix and converting to lower case.
	/// </summary>
	/// <param name="requestUri">The original request URI.</param>
	/// <returns>The formatted URI string.</returns>
	protected virtual string FormatRequestUri(string requestUri)
		=> requestUri.Remove("Async").ToLowerInvariant();

	/// <summary>
	/// Converts the caller method information into a request URI.
	/// </summary>
	/// <param name="callerMethod">The caller method info.</param>
	/// <returns>The formatted request URI string.</returns>
	protected virtual string ToRequestUri(MethodInfo callerMethod)
	{
		var requestUri = callerMethod.Name;

		var idx = requestUri.LastIndexOf('.');

		// explicit interface implemented method
		if (idx != -1)
			requestUri = requestUri.Substring(idx + 1);

		return FormatRequestUri(requestUri);
	}

	/// <summary>
	/// Retrieves information for the API method call, including the absolute URL, parameter list, and caller method info.
	/// </summary>
	/// <param name="method">The HTTP method to use.</param>
	/// <param name="methodName">The method name corresponding to the API endpoint.</param>
	/// <param name="args">The arguments to pass to the API method.</param>
	/// <returns>A tuple containing the absolute URL, parameters, and caller method info.</returns>
	protected virtual (Url url, (string name, object value)[] parameters, MethodInfo callerMethod) GetInfo(HttpMethod method, string methodName, object[] args)
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		if (args is null)
			throw new ArgumentNullException(nameof(args));

		var callerMethod = _methodsCache.SafeAdd((GetType(), methodName), key =>
		{
			var methods = key.Item1.GetMembers<MethodInfo>(BindingFlags.Public | BindingFlags.Instance, true, key.methodName, null);

			MethodInfo callerMethod;

			if (methods.Length > 1)
			{
				callerMethod = methods.FirstOrDefault(m =>
				{
					var parameters = m.GetParameters();

					var count = parameters.Length;

					if (count > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
						count--;

					return count == args.Length;
				});
			}
			else
				callerMethod = methods.FirstOrDefault();

			if (callerMethod is null)
				throw new ArgumentException($"Method {key.methodName} not exist.", nameof(key));

			return callerMethod;
		});
		
		var methodAttr = callerMethod.GetAttribute<RestAttribute>();
		var parameters = callerMethod.GetParameters();
		var paramsEnum = parameters
			.Select(pi => (pi, attr: pi.GetAttribute<RestAttribute>()))
			.Where(t => t.attr?.Ignore != true);

		if (parameters.Length > 0)
		{
			var last = parameters.Last();

			if (last.ParameterType == typeof(CancellationToken))
				paramsEnum = paramsEnum.SkipLast(1);
		}

		var paramsArr = paramsEnum.ToArray();

		if (args.Length != paramsArr.Length)
			throw new ArgumentOutOfRangeException(nameof(args), $"Args='{args.Select(a => a.To<string>()).JoinCommaSpace()}' != Params='{paramsArr.Select(t => t.pi.Name).JoinCommaSpace()}'");

		List<(string name, object value)> list = [];

		var i = 0;
		foreach (var (pi, attr) in paramsArr)
		{
			var arg = args[i++];

			list.Add(((attr?.Name).IsEmpty(FormatArgName(pi.Name)), TryFormat(arg, callerMethod, method)));
		}

		var url = GetAbsolute(methodAttr is null ? ToRequestUri(callerMethod) : methodAttr.Name);

		return (url, list.ToArray(), callerMethod);
	}

	/// <summary>
	/// Combines the base address with a relative URI to produce an absolute URL.
	/// </summary>
	/// <param name="relative">The relative URI.</param>
	/// <returns>The absolute <see cref="Url"/>.</returns>
	protected Url GetAbsolute(string relative)
		=> new(BaseAddress, relative);

	/// <summary>
	/// Formats the argument name.
	/// </summary>
	/// <param name="argName">The original argument name.</param>
	/// <returns>The formatted argument name.</returns>
	protected virtual string FormatArgName(string argName)
		=> argName;

	/// <summary>
	/// Tries to format the argument based on its type.
	/// Converts enumerations and booleans to their numeric representation.
	/// </summary>
	/// <param name="arg">The argument to format.</param>
	/// <param name="callerMethod">The caller method information.</param>
	/// <param name="method">The HTTP method.</param>
	/// <returns>The formatted argument.</returns>
	protected virtual object TryFormat(object arg, MethodInfo callerMethod, HttpMethod method)
		=> (arg is Enum || arg is bool) ? arg.To<long>() : arg;
}