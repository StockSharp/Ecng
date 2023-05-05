namespace Ecng.Net;

using System.Net.Http.Headers;
using System.Reflection;
using System.Diagnostics;

using Ecng.Reflection;

using Microsoft.Net.Http.Headers;

public abstract class RestBaseApiClient
{
	private readonly HttpMessageInvoker _http;
	private readonly MediaTypeFormatter _request;
	private readonly MediaTypeFormatter _response;

	protected RestBaseApiClient(HttpMessageInvoker http, MediaTypeFormatter request, MediaTypeFormatter response)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
		_request = request ?? throw new ArgumentNullException(nameof(request));
		_response = response ?? throw new ArgumentNullException(nameof(response));
	}

	protected Uri BaseAddress { get; set; }

	public IDictionary<string, string> PerRequestHeaders { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
	public IRestApiClientCache Cache { get; set; }

	public Action<string> LogBadResponse { get; set; }

	protected virtual bool PlainSingleArg => true;
	protected virtual bool ThrowIfNonSuccessStatusCode => true;

	protected void AddAuth(AuthenticationSchemes schema, string value)
		=> AddAuth(schema.ToString(), value);

	protected void AddAuthBearer(string token)
		=> AddAuth("Bearer", token);

	protected void AddAuth(string schema, string value)
		=> PerRequestHeaders.Add(HeaderNames.Authorization, $"{schema} {value}");

	protected virtual object FormatRequest(IDictionary<string, object> parameters)
		=> parameters;

	public bool Tracing { get; set; }

	protected virtual void TraceCall(HttpMethod method, Uri uri, TimeSpan elapsed)
	{
		Trace.WriteLine($"{method} {uri}: {elapsed}");
	}

	protected virtual async Task ValidateResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response is null)
			throw new ArgumentNullException(nameof(response));

		if (!ThrowIfNonSuccessStatusCode || response.IsSuccessStatusCode)
			return;

		var evt = LogBadResponse;

		if (evt is not null)
		{
			evt(await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
						cancellationToken
#endif
			));
		}

		response.EnsureSuccessStatusCode();
	}

	protected virtual Task<TResult> GetResultAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		return typeof(TResult) == typeof(VoidType)
			? default(TResult).FromResult()
			: response.Content.ReadAsAsync<TResult>(new[] { _response }, cancellationToken);
	}

	private async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, IRestApiClientCache cache, CancellationToken cancellationToken)
	{
		if (cache != null && cache.TryGet<TResult>(method, uri, body, out var cached))
			return cached;

		var request = new HttpRequestMessage(method, uri);

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_request.SupportedMediaTypes.First().MediaType));

		if (PerRequestHeaders.Count > 0)
		{
			foreach (var pair in PerRequestHeaders)
				request.Headers.Add(pair.Key, pair.Value);
		}

		if (body is not null)
			request.Content = new ObjectContent<object>(body, _request);

		var watch = Tracing ? Stopwatch.StartNew() : null;

		var response = await _http.SendAsync(request, cancellationToken);

		await ValidateResponseAsync(response, cancellationToken);

		var result = await GetResultAsync<TResult>(response, cancellationToken);

		if (watch is not null)
		{
			watch.Stop();
			TraceCall(method, uri, watch.Elapsed);
		}

		cache?.Set(method, uri, body, result);
		return result;
	}

	protected Task<TResult> PostAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Post;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		object body;

		if (parameters.Length > 1 || !PlainSingleArg)
		{
			var dict = new Dictionary<string, object>();

			foreach (var (name, value, required) in parameters)
			{
				if (required)
					dict.Add(name, value);
			}

			body = FormatRequest(dict);
		}
		else
			body = TryFormat(parameters.FirstOrDefault().value, url, method);

		return DoAsync<TResult>(method, url, body, Cache, cancellationToken);
	}

	protected Task<TResult> GetAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Get;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		if (parameters.Length > 0)
		{
			foreach (var (name, value, required) in parameters)
			{
				if (required)
					url.QueryString.Append(name, value?.ToString().EncodeToHtml());
			}
		}

		return DoAsync<TResult>(method, url, null, Cache, cancellationToken);
	}

	protected Task<TResult> DeleteAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Delete;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		if (parameters.Length > 0)
		{
			foreach (var (name, value, required) in parameters)
			{
				if (required)
					url.QueryString.Append(name, value?.ToString().EncodeToHtml());
			}
		}

		return DoAsync<TResult>(method, url, null, Cache, cancellationToken);
	}

	protected Task<TResult> PutAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Put;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		object body;

		if (parameters.Length > 1 || !PlainSingleArg)
		{
			var dict = new Dictionary<string, object>();

			foreach (var (name, value, required) in parameters)
			{
				if (required)
					dict.Add(name, value);
			}

			body = FormatRequest(dict);
		}
		else
			body = TryFormat(parameters.FirstOrDefault().value, url, method);

		return DoAsync<TResult>(method, url, body, Cache, cancellationToken);
	}

	protected virtual string FormatRequestUri(string requestUri)
		=> requestUri.Remove("Async").ToLowerInvariant();

	protected static MethodInfo GetCurrentMethod()
		=> (MethodInfo)new StackTrace().GetFrame(1).GetMethod();

	protected virtual (Url url, (string name, object value, bool required)[] parameters) GetInfo(HttpMethod method, MethodInfo callerMethod, object[] args)
	{
		if (callerMethod is null)
			throw new ArgumentNullException(nameof(callerMethod));

		if (args is null)
			throw new ArgumentNullException(nameof(args));

		var methodAttr = callerMethod.GetAttribute<RestApiMethodAttribute>();
		var parameters = callerMethod.GetParameters();

		if (parameters.Length > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
			parameters = parameters.Take(parameters.Length - 1).ToArray();

		if (args.Length != parameters.Length)
			throw new ArgumentOutOfRangeException(nameof(args));

		var url = new Url(BaseAddress, methodAttr is null ? FormatRequestUri(callerMethod.Name) : methodAttr.Name);

		List<(string name, object value, bool required)> list = new();

		var i = 0;
		foreach (var pi in parameters)
		{
			var paramAttr = pi.GetAttribute<RestApiParamAttribute>();
			var arg = args[i++];

			var required = true;

			if (paramAttr?.IsRequired != true)
			{
				if (pi.DefaultValue is null && arg is null)
					required = false;
				else if (pi.DefaultValue is not null && arg is not null && pi.DefaultValue.Equals(arg))
					required = false;
			}

			list.Add(((paramAttr?.Name).IsEmpty(pi.Name), TryFormat(arg, url, method), required));
		}

		return (url, list.ToArray());
	}

	protected virtual object TryFormat(object arg, Url url, HttpMethod method)
		=> (arg is Enum || arg is bool) ? arg.To<long>() : arg;
}