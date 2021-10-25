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
	using Ecng.Collections;

	public abstract class RestBaseApiClient : Disposable
	{
		private readonly HttpClient _client;
		private readonly MediaTypeFormatter _request;
		private readonly MediaTypeFormatter _response;

		protected RestBaseApiClient(Uri baseAddress,
			MediaTypeFormatter request,
			MediaTypeFormatter response)
		{
			if (baseAddress is null)
				throw new ArgumentNullException(nameof(baseAddress));

			_client = new() { BaseAddress = baseAddress };

			_request = request ?? throw new ArgumentNullException(nameof(request));
			_response = response ?? throw new ArgumentNullException(nameof(response));

			//DefaultRequestHeaders.Accept.Clear();
			DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(request.SupportedMediaTypes.First().MediaType));
		}

		protected Uri BaseAddress
		{
			get => _client.BaseAddress;
			set => _client.BaseAddress = value;
		}

		protected HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

		protected override void DisposeManaged()
		{
			_client.Dispose();

			base.DisposeManaged();
		}

		private async Task<TOutput> GetResultAsync<TOutput>(HttpResponseMessage response, CancellationToken cancellationToken)
		{
			response.EnsureSuccessStatusCode();

			if (typeof(TOutput) == typeof(VoidType))
				return default;

			return await response.Content.ReadAsAsync<TOutput>(new[] { _response }, cancellationToken);
		}

		protected virtual object FormatRequest(IDictionary<string, object> parameters)
			=> parameters;

		protected async Task<TOutput> PostAsync<TOutput>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			var body = new Dictionary<string, object>();

			foreach (var (_, isRequired, name, value) in parameters)
			{
				if (value is null && !isRequired)
					continue;

				body.Add(name, TryFormat(value));
			}

			using var response = await _client.PostAsync(url, FormatRequest(body), _request, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> GetAsync<TOutput>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			if (parameters.Length > 0)
			{
				url = $"{url}?" + parameters
					.Select(p => ((p.value is null || (p.info.ParameterType == typeof(bool) && !(bool)p.value)) && !p.isRequired) ? null : $"{p.name}={TryFormat(p.value)?.ToString().EncodeToHtml()}")
					.Where(s => s != null)
					.JoinAnd();
			}

			using var response = await _client.GetAsync(url, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> DeleteAsync<TOutput>(string requestUri, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(requestUri, args);

			if (parameters.Length > 0)
			{
				url = $"{url}?" + parameters
					.Select(p => ((p.value is null || (p.info.ParameterType == typeof(bool) && !(bool)p.value)) && !p.isRequired) ? null : $"{p.name}={TryFormat(p.value)?.ToString().EncodeToHtml()}")
					.Where(s => s != null)
					.JoinAnd();
			}

			using var response = await _client.DeleteAsync(url, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> PutAsync<TInput, TOutput>(string requestUri, TInput value, CancellationToken cancellationToken)
		{
			using var response = await _client.PutAsync(requestUri, value, _request, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected virtual string FormatRequestUri(string requestUri)
			=> requestUri.Remove("Async").ToLowerInvariant();

		protected static string GetCurrentMethod([CallerMemberName]string methodName = "")
			=> methodName;

		private (string url, (ParameterInfo info, bool isRequired, string name, object value)[] parameters) GetInfo(string requestUri, object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			var callerMethod = GetType().GetMember<MethodInfo>(requestUri, BindingFlags.Public | BindingFlags.Instance, null);
			var parameters = callerMethod.GetParameters();

			if (parameters.Length > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
				parameters = parameters.Take(parameters.Length - 1).ToArray();

			if (args.Length != parameters.Length)
				throw new ArgumentOutOfRangeException(nameof(args));

			return (FormatRequestUri(requestUri), parameters.Select((pi, i) =>
			{
				var attr = pi.GetAttribute<RestApiParamAttribute>();
				return (pi, attr?.IsRequired == true, (attr?.Name).IsEmpty(pi.Name), args[i]);
			}).ToArray());
		}

		protected virtual object TryFormat(object arg) => arg;
	}
}