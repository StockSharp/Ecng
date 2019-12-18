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
		public static void AddBodyAsStr(this IRestRequest request, string bodyStr)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			request.AddParameter(request.JsonSerializer.ContentType, bodyStr, ParameterType.RequestBody);
		}

		public static object Invoke(this IRestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Action<IRestClient> init = null, Func<string, string> contentConverter = null)
		{
			return request.Invoke<object>(url, caller, logVerbose, init, contentConverter);
		}

		public static T Invoke<T>(this IRestRequest request, Uri url, object caller, Action<string, object[]> logVerbose, Action<IRestClient> init = null, Func<string, string> contentConverter = null)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			if (url == null)
				throw new ArgumentNullException(nameof(url));

			if (caller == null)
				throw new ArgumentNullException(nameof(caller));

			var asm = caller.GetType().Assembly;
			var projName = asm.GetAttribute<AssemblyProductAttribute>()?.Product;

			var client = new RestClient(url)
			{
				UserAgent = $"{projName}/" + asm.GetName().Version
			};

			init?.Invoke(client);

			logVerbose?.Invoke("Request '{0}' Args '{1}'.", new object[] { url, request.Parameters.Select(p => $"{p.Name}={p.Value}").Join("&") });
			var response = client.Execute(request);

			var content = response.Content;
			logVerbose?.Invoke("Response '{0}' (code {1}).", new object[] { content, response.StatusCode });

			if (response.StatusCode != HttpStatusCode.OK)
				throw new InvalidOperationException(content.IsEmpty() ? (response.ErrorMessage.IsEmpty() ? response.StatusDescription : response.ErrorMessage) : content);

			if (content.IsEmpty())
				throw new InvalidOperationException();

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