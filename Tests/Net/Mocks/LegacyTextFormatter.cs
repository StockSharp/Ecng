namespace Ecng.Tests.Net.Mocks;

using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Exact copy of the old TextMediaTypeFormatter (pre-refactor)
/// that inherited from Microsoft's MediaTypeFormatter.
/// Used for side-by-side comparison tests.
/// </summary>
internal class LegacyTextFormatter : MediaTypeFormatter
{
	public LegacyTextFormatter(IEnumerable<string> mediaTypes)
	{
		if (mediaTypes is null)
			throw new ArgumentNullException(nameof(mediaTypes));

		foreach (var mediaType in mediaTypes)
			SupportedMediaTypes.Add(new(mediaType));
	}

	public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
	{
		return ReadFromStreamAsync(type, readStream, content, formatterLogger, default);
	}

	public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken)
	{
		using var streamReader = new StreamReader(readStream);
		var str = await streamReader.ReadToEndAsync(
#if NET7_0_OR_GREATER
			cancellationToken
#endif
		).NoWait();

		if (type == typeof(string))
			return str;

		return str.DeserializeObject(type);
	}

	public override bool CanReadType(Type type) => true;

	public override bool CanWriteType(Type type) => false;
}
