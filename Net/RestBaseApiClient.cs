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
		private readonly HttpClient _client;
		private readonly MediaTypeFormatter _request;
		private readonly MediaTypeFormatter _response;

		protected RestBaseApiClient(HttpClient client, MediaTypeFormatter request, MediaTypeFormatter response)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_request = request ?? throw new ArgumentNullException(nameof(request));
			_response = response ?? throw new ArgumentNullException(nameof(response));

			//Headers.Accept.Clear();
			Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(request.SupportedMediaTypes.First().MediaType));
		}

		protected Uri BaseAddress { get; set; }
		public HttpRequestHeaders Headers => _client.DefaultRequestHeaders;

		public IRestApiClientCache Cache { get; set; }

		private async Task<TResult> GetResultAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
		{
			response.EnsureSuccessStatusCode();

			if (typeof(TResult) == typeof(VoidType))
				return default;

			return await response.Content.ReadAsAsync<TResult>(new[] { _response }, cancellationToken);
		}

		protected virtual object FormatRequest(IDictionary<string, object> parameters)
			=> parameters;

		protected async Task<TResult> PostAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
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

			using var response = await _client.PostAsync(url, body, _request, cancellationToken);
			return await GetResultAsync<TResult>(response, cancellationToken);
		}

		protected async Task<TResult> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
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

			var cache = Cache;

			if (cache != null && cache.TryGet<TResult>(url, out var cached))
				return cached;

			using var response = await _client.GetAsync(url, cancellationToken);
			var result = await GetResultAsync<TResult>(response, cancellationToken);
			cache?.Set(url, result);
			return result;
		}

		protected async Task<TResult> DeleteAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
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

			using var response = await _client.DeleteAsync(url, cancellationToken);
			return await GetResultAsync<TResult>(response, cancellationToken);
		}

		protected async Task<TResult> PutAsync<TResult>(string requestUri, CancellationToken cancellationToken, params object[] args)
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

			using var response = await _client.PutAsync(url, body, _request, cancellationToken);
			return await GetResultAsync<TResult>(response, cancellationToken);
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