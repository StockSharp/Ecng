namespace Ecng.Net;

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;

using Ecng.Reflection;

using Microsoft.Net.Http.Headers;

public abstract class RestBaseApiClient
{
	private static readonly SynchronizedDictionary<(Type type, string methodName), MethodInfo> _methodsCache = new();

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

	protected virtual HttpRequestMessage GetRequest(HttpMethod method, Uri uri, object body)
	{
		var request = new HttpRequestMessage(method, uri);

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(RequestFormatter.SupportedMediaTypes.First().MediaType));

		if (PerRequestHeaders.Count > 0)
		{
			foreach (var pair in PerRequestHeaders)
				request.Headers.Add(pair.Key, pair.Value);
		}

		if (body is not null)
			request.Content = new ObjectContent<object>(body, RequestFormatter);

		return request;
	}

	protected async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, CancellationToken cancellationToken)
	{
		var cache = Cache;

		if (cache != null && cache.TryGet<TResult>(method, uri, body, out var cached))
			return cached;

		using var request = GetRequest(method, uri, body);

		var watch = Tracing ? Stopwatch.StartNew() : null;

		using var response = await Http.SendAsync(request, cancellationToken);

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

	protected Task<TResult> PostAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
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

		return DoAsync<TResult>(method, url, body, cancellationToken);
	}

	protected Task<TResult> GetAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
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

		return DoAsync<TResult>(method, url, null, cancellationToken);
	}

	protected Task<TResult> DeleteAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
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

		return DoAsync<TResult>(method, url, null, cancellationToken);
	}

	protected Task<TResult> PutAsync<TResult>(string methodName, CancellationToken cancellationToken, params object[] args)
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

		return DoAsync<TResult>(method, url, body, cancellationToken);
	}

	protected static string GetCurrentMethod([CallerMemberName]string methodName = "")
		=> methodName;

	protected virtual string FormatRequestUri(string requestUri)
		=> requestUri.Remove("Async").ToLowerInvariant();

	protected virtual string ToRequestUri(MethodInfo callerMethod)
	{
		var requestUri = callerMethod.Name;

		var idx = requestUri.LastIndexOf('.');

		// explicit interface implemented method
		if (idx != -1)
			requestUri = requestUri.Substring(idx + 1);

		return FormatRequestUri(requestUri);
	}

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

		List<(string name, object value)> list = new();

		var i = 0;
		foreach (var (pi, attr) in paramsArr)
		{
			var arg = args[i++];

			list.Add(((attr?.Name).IsEmpty(FormatArgName(pi.Name)), TryFormat(arg, callerMethod, method)));
		}

		var url = new Url(BaseAddress, methodAttr is null ? ToRequestUri(callerMethod) : methodAttr.Name);

		return (url, list.ToArray(), callerMethod);
	}

	protected virtual string FormatArgName(string argName)
		=> argName;

	protected virtual object TryFormat(object arg, MethodInfo callerMethod, HttpMethod method)
		=> (arg is Enum || arg is bool) ? arg.To<long>() : arg;
}