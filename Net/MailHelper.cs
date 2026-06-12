namespace Ecng.Net;

using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Provides helper methods to send emails and to manage mail attachments.
/// </summary>
#if NET7_0_OR_GREATER
public static partial class MailHelper
#else
public static class MailHelper
#endif
{
	private static readonly FieldInfo _attachmentNameField = GetAttachmentField("_name") ?? GetAttachmentField("name");
	private static readonly FieldInfo _attachmentNameEncodingField = GetAttachmentField("_nameEncoding") ?? GetAttachmentField("nameEncoding");

	/// <summary>
	/// Adds an HTML body alternate view to the specified <see cref="MailMessage"/>.
	/// </summary>
	/// <param name="message">The mail message to add the HTML body to.</param>
	/// <param name="bodyHtml">The HTML string representing the body content.</param>
	/// <returns>The updated <see cref="MailMessage"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
	public static MailMessage AddHtml(this MailMessage message, string bodyHtml)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyHtml, null, MediaTypeNames.Text.Html));

		return message;
	}

	/// <summary>
	/// Adds a plain text body alternate view to the specified <see cref="MailMessage"/>.
	/// </summary>
	/// <param name="message">The mail message to add the plain text body to.</param>
	/// <param name="bodyPlain">The plain text string representing the body content.</param>
	/// <returns>The updated <see cref="MailMessage"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
	public static MailMessage AddPlain(this MailMessage message, string bodyPlain)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyPlain, null, MediaTypeNames.Text.Plain));

		return message;
	}

#if NET7_0_OR_GREATER
	[GeneratedRegex(@"^([\w\.\+\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Singleline)]
	private static partial Regex EmailRegex();
#else
	private static readonly Regex _emailRegex = new(@"^([\w\.\+\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Compiled | RegexOptions.Singleline);
	private static Regex EmailRegex() => _emailRegex;
#endif

	/// <summary>
	/// Validates whether the specified email address string is in a correct format.
	/// </summary>
	/// <param name="email">The email address string to validate.</param>
	/// <returns><c>true</c> if the email format is valid; otherwise, <c>false</c>.</returns>
	public static bool IsEmailValid(this string email)
	{
		// https://stackoverflow.com/questions/5342375/regex-email-validation

		try
		{
			new MailAddress(email);
			return EmailRegex().IsMatch(email);
		}
		catch (FormatException)
		{
			return false;
		}
	}

	/// <summary>
	/// Attaches a file to the specified <see cref="MailMessage"/> using the provided stream.
	/// </summary>
	/// <param name="message">The mail message to attach the file to.</param>
	/// <param name="fileName">The name of the file to attach.</param>
	/// <param name="fileBody">The stream that represents the file content.</param>
	/// <returns>The updated <see cref="MailMessage"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
	public static MailMessage Attach(this MailMessage message, string fileName, Stream fileBody)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (!fileName.IsEmpty())
			message.Attachments.Add(ToAttachment(fileName, fileBody));

		return message;
	}

	/// <summary>
	/// Creates a new <see cref="Attachment"/> from the specified file name and file stream.
	/// </summary>
	/// <param name="fileName">The name of the file for the attachment.</param>
	/// <param name="fileBody">The stream representing the attachment content.</param>
	/// <returns>A new instance of <see cref="Attachment"/>.</returns>
	public static Attachment ToAttachment(string fileName, Stream fileBody)
	{
		return CreateAttachment(fileBody, fileName.ThrowIfEmpty(nameof(fileName)));
	}

	/// <summary>
	/// Creates an <see cref="Attachment"/> from the provided stream and display name, with specified transfer encoding.
	/// </summary>
	/// <param name="attachmentFile">The stream containing the attachment file.</param>
	/// <param name="displayName">The display name for the attachment.</param>
	/// <param name="transferEncoding">The transfer encoding to use. Defaults to Base64.</param>
	/// <returns>A new instance of <see cref="Attachment"/> configured with the provided parameters.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown if the specified <paramref name="transferEncoding"/> is not supported.
	/// </exception>
	public static Attachment CreateAttachment(Stream attachmentFile, string displayName, TransferEncoding transferEncoding = TransferEncoding.Base64)
	{
		var attachment = new Attachment(attachmentFile, string.Empty)
		{
			TransferEncoding = transferEncoding
		};

		string transferEncodingMarker;
		string encodingMarker;
		Encoding nameEncoding;
		int maxChunkLength;

		switch (transferEncoding)
		{
			case TransferEncoding.Base64:
				transferEncodingMarker = "B";
				encodingMarker = "UTF-8";
				nameEncoding = Encoding.UTF8;
				maxChunkLength = 30;
				break;
			case TransferEncoding.QuotedPrintable:
				transferEncodingMarker = "Q";
				encodingMarker = "UTF-8";
				nameEncoding = Encoding.UTF8;
				maxChunkLength = 76;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(transferEncoding), transferEncoding, "The specified TransferEncoding is not supported.");
		}

		attachment.NameEncoding = nameEncoding;

		var encodedName = EncodeAttachmentName(displayName, Encoding.GetEncoding(encodingMarker), transferEncodingMarker, maxChunkLength);
		attachment.Name = encodedName;
		attachment.ContentType.Parameters["name"] = encodedName;

		if (transferEncoding == TransferEncoding.QuotedPrintable)
		{
			_attachmentNameField?.SetValue(attachment, encodedName);
			_attachmentNameEncodingField?.SetValue(attachment, nameEncoding);
		}

		return attachment;
	}

	private static string EncodeAttachmentName(string displayName, Encoding encoding, string transferEncodingMarker, int maxChunkLength)
	{
		var encodingToken = $"=?{encoding.WebName.ToUpperInvariant()}?{transferEncodingMarker}?";
		const string softBreak = "?=";
		var maxPayloadLength = maxChunkLength - encodingToken.Length - softBreak.Length;

		var payloads = transferEncodingMarker == "B"
			? EncodeBase64Chunks(displayName, encoding, maxPayloadLength)
			: EncodeQuotedPrintableChunks(displayName, encoding, maxPayloadLength);

		return payloads.Select(p => encodingToken + p + softBreak).Join(" ");
	}

	private static IEnumerable<string> EncodeBase64Chunks(string value, Encoding encoding, int maxPayloadLength)
	{
		maxPayloadLength -= maxPayloadLength % 4;
		var maxBytes = Math.Max(1, maxPayloadLength / 4 * 3);
		var chunk = new StringBuilder();
		var chunkBytes = 0;

		foreach (var textElement in GetTextElements(value))
		{
			var bytes = encoding.GetByteCount(textElement);

			if (chunk.Length > 0 && chunkBytes + bytes > maxBytes)
			{
				yield return Convert.ToBase64String(encoding.GetBytes(chunk.ToString()));
				chunk.Clear();
				chunkBytes = 0;
			}

			chunk.Append(textElement);
			chunkBytes += bytes;
		}

		if (chunk.Length > 0)
			yield return Convert.ToBase64String(encoding.GetBytes(chunk.ToString()));
	}

	private static IEnumerable<string> EncodeQuotedPrintableChunks(string value, Encoding encoding, int maxPayloadLength)
	{
		var chunk = new StringBuilder();

		foreach (var textElement in GetTextElements(value))
		{
			var encoded = EncodeQuotedPrintableTextElement(textElement, encoding);

			if (chunk.Length > 0 && chunk.Length + encoded.Length > maxPayloadLength)
			{
				yield return chunk.ToString();
				chunk.Clear();
			}

			chunk.Append(encoded);
		}

		if (chunk.Length > 0)
			yield return chunk.ToString();
	}

	private static string EncodeQuotedPrintableTextElement(string value, Encoding encoding)
	{
		if (value == " ")
			return "_";

		var bytes = encoding.GetBytes(value);
		var sb = new StringBuilder();

		foreach (var b in bytes)
		{
			var c = (char)b;

			if (b is >= 33 and <= 126 && c is not '=' and not '?' and not '_')
				sb.Append(c);
			else
				sb.Append('=').Append(b.ToString("X2", CultureInfo.InvariantCulture));
		}

		return sb.ToString();
	}

	private static IEnumerable<string> GetTextElements(string value)
	{
		var enumerator = StringInfo.GetTextElementEnumerator(value);

		while (enumerator.MoveNext())
			yield return enumerator.GetTextElement();
	}

	private static FieldInfo GetAttachmentField(string name)
		=> typeof(Attachment).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
}
