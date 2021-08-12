namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Diagnostics;

	using Newtonsoft.Json;

	static class JsonHelper
	{
		[Conditional("DEBUG")]
		public static void ChechExpectedToken(this JsonReader reader, JsonToken token)
		{
			if (reader.TokenType != token)
				throw new InvalidOperationException($"{reader.TokenType} != {token}");
		}

		public static async Task ReadWithCheckAsync(this JsonReader reader, CancellationToken cancellationToken)
		{
			if (!await reader.ReadAsync(cancellationToken))
				throw new InvalidOperationException("EOF");
		}
	}
}