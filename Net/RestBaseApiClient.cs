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

		private async Task<TResult> DoAsync<TResult>(HttpMethod method, Uri uri, object body, IRestApiClientCache cache, CancellationToken cancellationToken)
		{
			if (cache != null && cache.TryGet<TResult>(uri, out var cached))
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

			var response = await _http.SendAsync(request, cancellationToken);

			response.EnsureSuccessStatusCode();

			var result = typeof(TResult) == typeof(VoidType)
				? default
				: await response.Content.ReadAsAsync<TResult>(new[] { _response }, cancellationToken);

			cache?.Set(uri, result);
			return result;
		}

		protected Task<TResult> PostAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			object body;

			if (parameters.Length > 1)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (_, isRequired, name, value) in parameters)
				{
					if (value is null && !isRequired)
						continue;

					dict.Add(name, TryFormat(value));
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value);

			return DoAsync<TResult>(HttpMethod.Post, url, body, null, cancellationToken);
		}

		protected Task<TResult> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			if (parameters.Length > 0)
			{
				foreach (var (info, isRequired, name, value) in parameters)
				{
					if ((value is null || (info.ParameterType == typeof(bool) && !(bool)value)) && !isRequired)
						continue;

					url.QueryString.Append(name, TryFormat(value)?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(HttpMethod.Get, url, null, Cache, cancellationToken);
		}

		protected Task<TResult> DeleteAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			if (parameters.Length > 0)
			{
				foreach (var (info, isRequired, name, value) in parameters)
				{
					if ((value is null || (info.ParameterType == typeof(bool) && !(bool)value)) && !isRequired)
						continue;

					url.QueryString.Append(name, TryFormat(value)?.ToString().EncodeToHtml());
				}
			}

			return DoAsync<TResult>(HttpMethod.Delete, url, null, null, cancellationToken);
		}

		protected Task<TResult> PutAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			object body;

			if (parameters.Length > 1)
			{
				var dict = new Dictionary<string, object>();

				foreach (var (_, isRequired, name, value) in parameters)
				{
					if (value is null && !isRequired)
						continue;

					dict.Add(name, TryFormat(value));
				}

				body = FormatRequest(dict);
			}
			else
				body = TryFormat(parameters.FirstOrDefault().value);

			return DoAsync<TResult>(HttpMethod.Put, url, body, null, cancellationToken);
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

		protected virtual object TryFormat(object arg) => arg;
	}
}