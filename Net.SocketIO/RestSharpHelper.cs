namespace Ecng.Net
{
	using System;
	using System.Linq;
	using System.Net;
	using System.Reflection;

	using Ecng.Common;

	using RestSharp;

	public static class RestSharpHelper
	{
		public class UnexpectedResponseError : InvalidOperationException
		{
			public IRestResponse Response { get; }

			public UnexpectedResponseError(IRestResponse response) : base($"unexpected response code='{response.StatusCode}', msg='{response.ErrorMessage}', desc='{response.StatusDescription}', content='{response.Content}'")
				=> Response = response;
		}

		public static void AddBodyAsStr(this IRestRequest request, string bodyStr)
		{
			if (request is null)
				throw new ArgumentNullException(nameof(request));

			request.AddParameter(RestSharp.Serialization.ContentType.Json, bodyStr, ParameterType.RequestBody);
		}

		public static object Invoke(this IRestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Action<IRestClient> init = null, Func<string, string> contentConverter = null)
		{
			return request.Invoke<object>(url, caller, logVerbose, init, contentConverter);
		}

		public static T Invoke<T>(this IRestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Action<IRestClient> init = null, Func<string, string> contentConverter = null)
		{
			if (request is null)
				throw new ArgumentNullException(nameof(request));

			if (url is null)
				throw new ArgumentNullException(nameof(url));

			if (caller is null)
				throw new ArgumentNullException(nameof(caller));

			var asm = caller.GetType().Assembly;
			var projName = asm.GetAttribute<AssemblyProductAttribute>()?.Product;

			var client = new RestClient(url)
			{
				UserAgent = $"{projName}/" + asm.GetName().Version
			};

			init?.Invoke(client);

			logVerbose?.Invoke("Request {0}, '{1}' Args '{2}'.", new object[] { request.Method, url, request.Parameters.Select(p => $"{p.Name}={p.Value}").JoinAnd() });
			var response = client.Execute(request);

			var content = response.Content;
			logVerbose?.Invoke("Response '{0}' (code {1}).", new object[] { content, response.StatusCode });

			// https://restsharp.dev/usage/exceptions.html
			var networkFailure = response.ResponseStatus != ResponseStatus.Completed;
			if(networkFailure)
				throw new InvalidOperationException($"failed to complete request: {response.ResponseStatus}");

			if (response.StatusCode != HttpStatusCode.OK || content.IsEmpty())
				throw new UnexpectedResponseError(response);

			if (contentConverter != null)
			{
				content = contentConverter(content);

				if (content.IsEmpty())
					return default;
			}

			return content.DeserializeObject<T>();
		}
	}
}