namespace Ecng.Net;

using System.IO;

using Ecng.Serialization;

/// <summary>
/// A media type formatter for processing text-based content.
/// </summary>
public class TextMediaTypeFormatter : MediaTypeFormatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TextMediaTypeFormatter"/> class with the specified media types.
	/// </summary>
	/// <param name="mediaTypes">A collection of media type strings.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mediaTypes"/> is null.</exception>
	public TextMediaTypeFormatter(IEnumerable<string> mediaTypes)
	{
		if (mediaTypes is null)
			throw new ArgumentNullException(nameof(mediaTypes));

		foreach (var mediaType in mediaTypes)
			SupportedMediaTypes.Add(new(mediaType));
	}

	/// <summary>
	/// Asynchronously reads an object from the specified stream.
	/// This overload calls the overload with a cancellation token.
	/// </summary>
	/// <param name="type">The type of the object to deserialize.</param>
	/// <param name="readStream">The stream to read from.</param>
	/// <param name="content">The HTTP content.</param>
	/// <param name="formatterLogger">The formatter logger for collecting errors.</param>
	/// <returns>A task that represents the asynchronous read operation. The task result contains the deserialized object.</returns>
	public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
	{
		return ReadFromStreamAsync(type, readStream, content, formatterLogger, default);
	}

	/// <summary>
	/// Asynchronously reads an object from the specified stream using the provided cancellation token.
	/// </summary>
	/// <param name="type">The type of the object to deserialize.</param>
	/// <param name="readStream">The stream to read from.</param>
	/// <param name="content">The HTTP content.</param>
	/// <param name="formatterLogger">The formatter logger for collecting errors.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous read operation. The task result contains the deserialized object.</returns>
	public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken)
	{
		using var streamReader = new StreamReader(readStream);
		var str = await streamReader.ReadToEndAsync().NoWait();

		if (type == typeof(string))
			return str;

		return str.DeserializeObject(type);
	}

	/// <summary>
	/// Determines whether the formatter can read objects of the specified type.
	/// </summary>
	/// <param name="type">The type to test for read support.</param>
	/// <returns><c>true</c> if the type can be read; otherwise, <c>false</c>.</returns>
	public override bool CanReadType(Type type) => true;

	/// <summary>
	/// Determines whether the formatter can write objects of the specified type.
	/// </summary>
	/// <param name="type">The type to test for write support.</param>
	/// <returns><c>false</c> as this formatter does not support writing.</returns>
	public override bool CanWriteType(Type type) => false;
}