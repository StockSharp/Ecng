namespace Ecng.Net;

using System.IO;

using Ecng.Serialization;

public class TextMediaTypeFormatter : MediaTypeFormatter
{
	public TextMediaTypeFormatter(IEnumerable<string> mediaTypes)
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
		var str = await streamReader.ReadToEndAsync();

		if (type == typeof(string))
			return str;

		return str.DeserializeObject(type);
	}

	public override bool CanReadType(Type type) => true;
	public override bool CanWriteType(Type type) => false;
}