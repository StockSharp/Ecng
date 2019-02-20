namespace Ecng.Net
{
	using System;

	using RestSharp;

	public static class RestSharpHelper
	{
		public static void AddBodyAsStr(this IRestRequest request, string bodyStr)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			request.AddParameter(request.JsonSerializer.ContentType, bodyStr, ParameterType.RequestBody);
		}
	}
}