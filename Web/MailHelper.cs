namespace Ecng.Web
{
	using System;
	using System.IO;
	using System.Net.Mail;
	using System.Net.Mime;
	using System.Text;
	using System.Web;

	using Ecng.Common;

	public static class MailHelper
	{
		public static MailMessage Attach(this MailMessage message, IWebFile file)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (file != null)
				message.Attachments.Add(file.ToAttachment());

			return message;
		}

		public static Attachment ToAttachment(this IWebFile file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			return CreateAttachment(file.Body.To<Stream>(), file.Name);
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
					throw new ArgumentException("The specified TransferEncoding is not supported: {0}".Put(transferEncoding), nameof(transferEncoding));
			}

			attachment.NameEncoding = Encoding.GetEncoding(encodingMarker);

			var encodingtoken = "=?{0}?{1}?".Put(encodingMarker, tranferEncodingMarker);

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
