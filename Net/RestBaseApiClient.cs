namespace Ecng.Net
{
	using System;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Net.Http.Formatting;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;

	using Ecng.Common;
	using Ecng.Reflection;

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

		protected virtual object FormatRequest(IDictionary<string, object> parameters)
			=> parameters;

		public bool Tracing { get; set; }

		protected virtual void TraceCall(HttpMethod method, Uri uri, TimeSpan elapsed)
		{
			Trace.WriteLine($"{method} {uri}: {elapsed}");
		}

		protected virtual Task ValidateResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
		{
			if (response is null)
				throw new ArgumentNullException(nameof(response));

			response.EnsureSuccessStatusCode();
			return Task.CompletedTask;
		}

		private async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, IRestApiClientCache cache, CancellationToken cancellationToken)
		{
			if (cache != null && cache.TryGet<TResult>(method, uri, out var cached))
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

			var watch = Tracing ? new Stopwatch() : null;
			watch?.Start();

			var response = await _http.SendAsync(request, cancellationToken);

			await ValidateResponseAsync(response, cancellationToken);

			var result = typeof(TResult) == typeof(VoidType)
				? default
				: await response.Content.ReadAsAsync<TResult>(new[] { _response }, cancellationToken);

			if (watch is not null)
			{
				watch.Stop();
				TraceCall(method, uri, watch.Elapsed);
			}

			cache?.Set(method, uri, result);
			return result;
		}

		protected Task<TResult> PostAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);
			var method = HttpMethod.Post;

			object body;

			if (parameters.Length > 1)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (_, isRequired, name, value) in parameters)
				{
					if (value is null && !isRequired)
						continue;

					dict.Add(name, TryFormat(value, method));
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value, method);

			return DoAsync<TResult>(method, url, body, Cache, cancellationToken);
		}

		protected Task<TResult> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);
			var method = HttpMethod.Get;

			if (parameters.Length > 0)
			{
				foreach (var (info, isRequired, name, value) in parameters)
				{
					if ((value is null || (info.ParameterType == typeof(bool) && !(bool)value)) && !isRequired)
						continue;

					url.QueryString.Append(name, TryFormat(value, method)?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(method, url, null, Cache, cancellationToken);
		}

		protected Task<TResult> DeleteAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);
			var method = HttpMethod.Delete;

			if (parameters.Length > 0)
			{
				foreach (var (info, isRequired, name, value) in parameters)
				{
					if ((value is null || (info.ParameterType == typeof(bool) && !(bool)value)) && !isRequired)
						continue;

					url.QueryString.Append(name, TryFormat(value, method)?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(method, url, null, Cache, cancellationToken);
		}

		protected Task<TResult> PutAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);
			var method = HttpMethod.Put;

			object body;

			if (parameters.Length > 1)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (_, isRequired, name, value) in parameters)
				{
					if (value is null && !isRequired)
						continue;

					dict.Add(name, TryFormat(value, method));
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value, method);

			return DoAsync<TResult>(method, url, body, Cache, cancellationToken);
		}

		protected virtual string FormatRequestUri(string requestUri)
			=> requestUri.Remove("Async").ToLowerInvariant();

		protected static string GetCurrentMethod([CallerMemberName]string methodName = "")
			=> methodName;

		private (Url url, (ParameterInfo info, bool isRequired, string name, object value)[] parameters) GetInfo(string requestUri, object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			var methods = GetType().GetMembers<MethodInfo>(BindingFlags.Public | BindingFlags.Instance, true, requestUri, null);

			MethodInfo callerMethod;

			if (methods.Length > 1)
			{
				callerMethod = methods.First(m =>
				{
					var parameters = m.GetParameters();

					var count = parameters.Length;

					if (count > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
						count--;

					return count == args.Length;
				});
			}
			else
				callerMethod = methods.First();

			var parameters = callerMethod.GetParameters();

			if (parameters.Length > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
				parameters = parameters.Take(parameters.Length - 1).ToArray();

			if (args.Length != parameters.Length)
				throw new ArgumentOutOfRangeException(nameof(args));

			return (new Url(BaseAddress, FormatRequestUri(requestUri)), parameters.Select((pi, i) =>
			{
				var attr = pi.GetAttribute<RestApiParamAttribute>();
				return (pi, attr?.IsRequired == true, (attr?.Name).IsEmpty(pi.Name), args[i]);
			}).ToArray());
		}

		protected virtual object TryFormat(object arg, HttpMethod method) => arg;
	}
}