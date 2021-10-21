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

		protected virtual string GetName(ParameterInfo pi)
			=> (pi.GetAttribute<RestApiParamAttribute>()?.Name).IsEmpty(pi.Name);

		protected virtual object FormatRequest(IDictionary<string, object> parameters)
			=> parameters;

		protected async Task<TOutput> PostAsync<TOutput>(string methodName, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(methodName, args);

			var body = new Dictionary<string, object>();

			if (parameters.Length > 0)
			{
				var i = 0;

				foreach (var p in parameters)
				{
					var arg = args[i];
					i++;

					if (arg is null && p.GetAttribute<RestApiParamAttribute>()?.IsRequired != true)
						continue;

					body.Add(GetName(p), TryFormat(arg));
				}
			}

			var response = await _client.PostAsync(url, FormatRequest(body), _request, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> GetAsync<TOutput>(string methodName, CancellationToken cancellationToken, params object[] args)
		{
			var (url, parameters) = GetInfo(methodName, args);

			if (parameters.Length > 0)
			{
				url = $"{url}?" + parameters
					.Select((p, i) => ((args[i] is null || (p.ParameterType == typeof(bool) && !(bool)args[i])) && p.GetAttribute<RestApiParamAttribute>()?.IsRequired != true) ? null : $"{GetName(p)}={TryFormat(args[i])?.ToString().EncodeToHtml()}")
					.Where(s => s != null)
					.JoinAnd();
			}

			var response = await _client.GetAsync(url, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> DeleteAsync<TOutput>(string methodName, CancellationToken cancellationToken)
		{
			var response = await _client.DeleteAsync(methodName, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected async Task<TOutput> PutAsync<TInput, TOutput>(string methodName, TInput value, CancellationToken cancellationToken)
		{
			var response = await _client.PutAsync(methodName, value, _request, cancellationToken);
			return await GetResultAsync<TOutput>(response, cancellationToken);
		}

		protected virtual string FormatMethodName(string methodName)
			=> methodName.Remove("Async").ToLowerInvariant();

		protected static string GetCurrentMethod([CallerMemberName]string methodName = "")
			=> methodName;

		private (string url, ParameterInfo[] parameters) GetInfo(string methodName, object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			var callerMethod = GetType().GetMember<MethodInfo>(methodName);
			var parameters = callerMethod.GetParameters();

			if (parameters.Length > 0 && parameters.Last().ParameterType == typeof(CancellationToken))
				parameters = parameters.Take(parameters.Length - 1).ToArray();

			if (args.Length != parameters.Length)
				throw new ArgumentOutOfRangeException(nameof(args));

			return (FormatMethodName(methodName), parameters);
		}

		protected virtual object TryFormat(object arg) => arg;
	}
}