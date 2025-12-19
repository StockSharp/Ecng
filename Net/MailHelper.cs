namespace Ecng.Net;

using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// Provides helper methods to send emails and to manage mail attachments.
/// </summary>
#if NET7_0_OR_GREATER
public static partial class MailHelper
#else
public static class MailHelper
#endif
{
	/// <summary>
	/// Sends the specified <see cref="MailMessage"/> synchronously.
	/// </summary>
	/// <param name="message">The mail message to send.</param>
	/// <param name="dispose">If set to <c>true</c>, disposes the mail message after sending.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
	[Obsolete("Synchronous sending is obsolete. Use SendAsync instead.")]
	public static void Send(this MailMessage message, bool dispose = true)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		using (var mail = new SmtpClient())
			mail.Send(message);

		if (dispose)
			message.Dispose();
	}

	/// <summary>
	/// Sends the specified <see cref="MailMessage"/> asynchronously.
	/// </summary>
	/// <param name="message">The mail message to send.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
	[Obsolete("Use SendMailAsync extension method of SmtpClient instead.")]
	public static async Task SendAsync(this MailMessage message, CancellationToken cancellationToken = default)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		using var mail = new SmtpClient();
		await mail.SendMailAsync(message
#if NET5_0_OR_GREATER
			, cancellationToken
#endif
		).NoWait();
	}

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
	[GeneratedRegex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Singleline)]
	private static partial Regex EmailRegex();
#else
	private static readonly Regex _emailRegex = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Compiled | RegexOptions.Singleline);
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
		int maxChunkLength;

		switch (transferEncoding)
		{
			case TransferEncoding.Base64:
				transferEncodingMarker = "B";
				encodingMarker = "UTF-8";
				maxChunkLength = 30;
				break;
			case TransferEncoding.QuotedPrintable:
				transferEncodingMarker = "Q";
				encodingMarker = "ISO-8859-1";
				maxChunkLength = 76;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(transferEncoding), transferEncoding, "The specified TransferEncoding is not supported.");
		}

		attachment.NameEncoding = Encoding.GetEncoding(encodingMarker);

		var encodingtoken = $"=?{encodingMarker}?{transferEncodingMarker}?";

		const string softbreak = "?=";

		var encodedAttachmentName = attachment.TransferEncoding == TransferEncoding.QuotedPrintable
			? HttpUtility.UrlEncode(displayName, Encoding.Default).Replace("+", " ").Replace("%", "=")
			: displayName.UTF8().Base64();

		encodedAttachmentName = SplitEncodedAttachmentName(encodingtoken, softbreak, maxChunkLength, encodedAttachmentName);
		attachment.Name = encodedAttachmentName;

		return attachment;
	}

	private static string SplitEncodedAttachmentName(string encodingtoken, string softbreak, int maxChunkLength, string encoded)
	{
		var splitLength = maxChunkLength - encodingtoken.Length - (softbreak.Length * 2);
		var parts = encoded.SplitByLength(splitLength);

		var encodedAttachmentName = encodingtoken;

		foreach (var part in parts)
			encodedAttachmentName += part + softbreak + encodingtoken;

		encodedAttachmentName = encodedAttachmentName.Remove(encodedAttachmentName.Length - encodingtoken.Length, encodingtoken.Length);
		return encodedAttachmentName;
	}
}
