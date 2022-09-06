namespace Ecng.Net
{
	using System;
	using System.IO;
	using System.Net.Mail;
	using System.Net.Mime;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Web;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Reflection;

	public static class MailHelper
	{
		public static void Send(this MailMessage message, bool dispose = true)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			using (var mail = new SmtpClient())
				mail.Send(message);

			if (dispose)
				message.Dispose();
		}

		public static async Task SendAsync(this MailMessage message, CancellationToken cancellationToken = default)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			using var mail = new SmtpClient();
			await mail.SendMailAsync(message
#if NET5_0_OR_GREATER
				, cancellationToken
#endif
			);
		}

		public static MailMessage AddHtml(this MailMessage message, string bodyHtml)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyHtml, null, MediaTypeNames.Text.Html));

			return message;
		}

		public static MailMessage AddPlain(this MailMessage message, string bodyPlain)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyPlain, null, MediaTypeNames.Text.Plain));

			return message;
		}

		// http://stackoverflow.com/a/9621399
		public static MemoryStream ToStream(this MailMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			var assembly = typeof(SmtpClient).Assembly;
			var mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");

			var stream = new MemoryStream();

			var mailWriter = mailWriterType.CreateInstance(new[] { (object)stream, true });

			message.SetValue<object, object[]>("Send", new[] { mailWriter, true, true });

			mailWriter.SetValue<object, VoidType>("Close", null);

			return stream;
		}

		private static readonly Regex _emailRegex1 = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Compiled | RegexOptions.Singleline);

		public static bool IsEmailValid(this string email)
		{
			// https://stackoverflow.com/questions/5342375/regex-email-validation

			try
			{
				new MailAddress(email);
				return _emailRegex1.IsMatch(email)/* && _emailRegex2.IsMatch(email)*/;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		public static MailMessage Attach(this MailMessage message, string fileName, Stream fileBody)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (!fileName.IsEmpty())
				message.Attachments.Add(ToAttachment(fileName, fileBody));

			return message;
		}

		public static Attachment ToAttachment(string fileName, Stream fileBody)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			return CreateAttachment(fileBody, fileName);
		}

		// http://social.msdn.microsoft.com/Forums/en-US/dotnetframeworkde/thread/b6c764f7-4697-4394-b45f-128a24306d55
		public static Attachment CreateAttachment(Stream attachmentFile, string displayName, TransferEncoding transferEncoding = TransferEncoding.Base64)
		{
			var attachment = new Attachment(attachmentFile, string.Empty)
			{
				TransferEncoding = transferEncoding
			};

			string tranferEncodingMarker;
			string encodingMarker;
			int maxChunkLength;

			switch (transferEncoding)
			{
				case TransferEncoding.Base64:
					tranferEncodingMarker = "B";
					encodingMarker = "UTF-8";
					maxChunkLength = 30;
					break;
				case TransferEncoding.QuotedPrintable:
					tranferEncodingMarker = "Q";
					encodingMarker = "ISO-8859-1";
					maxChunkLength = 76;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(transferEncoding), transferEncoding, "The specified TransferEncoding is not supported.");
			}

			attachment.NameEncoding = Encoding.GetEncoding(encodingMarker);

			var encodingtoken = $"=?{encodingMarker}?{tranferEncodingMarker}?";

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
}
