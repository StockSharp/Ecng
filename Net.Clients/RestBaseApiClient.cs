namespace Ecng.Net;

using System.Net.Http.Headers;
using System.Reflection;
using System.Diagnostics;

using Ecng.Reflection;

using Microsoft.Net.Http.Headers;

public abstract class RestBaseApiClient
{
	protected RestBaseApiClient(HttpMessageInvoker http, MediaTypeFormatter request, MediaTypeFormatter response)
	{
		Http = http ?? throw new ArgumentNullException(nameof(http));
		RequestFormatter = request ?? throw new ArgumentNullException(nameof(request));
		ResponseFormatter = response ?? throw new ArgumentNullException(nameof(response));
	}

	protected Uri BaseAddress { get; set; }

	protected HttpMessageInvoker Http { get; }
	protected MediaTypeFormatter RequestFormatter { get; }
	protected MediaTypeFormatter ResponseFormatter { get; }

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
			: response.Content.ReadAsAsync<TResult>(new[] { ResponseFormatter }, cancellationToken);
	}

	protected async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, CancellationToken cancellationToken)
	{
		var cache = Cache;

		if (cache != null && cache.TryGet<TResult>(method, uri, body, out var cached))
			return cached;

		var request = new HttpRequestMessage(method, uri);

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(RequestFormatter.SupportedMediaTypes.First().MediaType));

		if (PerRequestHeaders.Count > 0)
		{
			foreach (var pair in PerRequestHeaders)
				request.Headers.Add(pair.Key, pair.Value);
		}

		if (body is not null)
			request.Content = new ObjectContent<object>(body, RequestFormatter);

		var watch = Tracing ? Stopwatch.StartNew() : null;

		var response = await Http.SendAsync(request, cancellationToken);

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

			foreach (var (name, value) in parameters)
			{
				if (value is not null)
					dict.Add(name, value);
			}

			body = FormatRequest(dict);
		}
		else
			body = TryFormat(parameters.FirstOrDefault().value, callerMethod, method);

		return DoAsync<TResult>(method, url, body, cancellationToken);
	}

	protected Task<TResult> GetAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Get;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		if (parameters.Length > 0)
		{
			foreach (var (name, value) in parameters)
			{
				if (value is not null)
					url.QueryString.Append(name, value?.ToString().EncodeToHtml());
			}
		}

		return DoAsync<TResult>(method, url, null, cancellationToken);
	}

	protected Task<TResult> DeleteAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Delete;
		var (url, parameters) = GetInfo(method, callerMethod, args);

		if (parameters.Length > 0)
		{
			foreach (var (name, value) in parameters)
			{
				if (value is not null)
					url.QueryString.Append(name, value?.ToString().EncodeToHtml());
			}
		}

		return DoAsync<TResult>(method, url, null, cancellationToken);
	}

	protected Task<TResult> PutAsync<TResult>(MethodInfo callerMethod, CancellationToken cancellationToken, params object[] args)
	{
		var method = HttpMethod.Put;
		var (url, parameters) = GetInfo(method, callerMethod, args);

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

		return DoAsync<TResult>(method, url, body, cancellationToken);
	}

	protected static MethodInfo GetCurrentMethod(int frameIdx = 1)
		=> (MethodInfo)new StackTrace().GetFrame(frameIdx).GetMethod();

	protected virtual string ToRequestUri(MethodInfo callerMethod)
	{
		var requestUri = callerMethod.Name;

		var idx = requestUri.LastIndexOf('.');

		// explicit interface implemented method
		if (idx != -1)
			requestUri = requestUri.Substring(idx + 1);

		return requestUri.Remove("Async").ToLowerInvariant();
	}

	protected virtual (Url url, (string name, object value)[] parameters) GetInfo(HttpMethod method, MethodInfo callerMethod, object[] args)
	{
		if (callerMethod is null)
			throw new ArgumentNullException(nameof(callerMethod));

		if (args is null)
			throw new ArgumentNullException(nameof(args));

		var methodAttr = callerMethod.GetAttribute<RestApiMethodAttribute>();
		var parameters = callerMethod.GetParameters();
		var paramDict = parameters.ToDictionary(pi => pi, pi => pi.GetAttribute<RestApiParamAttribute>());

		if (parameters.Length > 0)
		{
			if (parameters.Last().ParameterType == typeof(CancellationToken))
				parameters = parameters.Take(parameters.Length - 1).ToArray();

			foreach (var pair in paramDict.Where(p => p.Value?.Ignore == true).ToArray())
				paramDict.Remove(pair.Key);
		}

		if (args.Length != parameters.Length)
			throw new ArgumentOutOfRangeException(nameof(args));

		List<(string name, object value)> list = new();

		var i = 0;
		foreach (var pair in paramDict)
		{
			var arg = args[i++];

			list.Add(((pair.Value?.Name).IsEmpty(pair.Key.Name), TryFormat(arg, callerMethod, method)));
		}

		var url = new Url(BaseAddress, methodAttr is null ? ToRequestUri(callerMethod) : methodAttr.Name);

		return (url, list.ToArray());
	}

	protected virtual object TryFormat(object arg, MethodInfo callerMethod, HttpMethod method)
		=> (arg is Enum || arg is bool) ? arg.To<long>() : arg;
}