namespace Ecng.Net
{
	using System;
	using System.IO;
	using System.Net.Mail;
	using System.Net.Mime;
	using System.Text.RegularExpressions;

	using Ecng.Reflection;

	public static class MailHelper
	{
		public static void Send(this MailMessage message, bool dispose = true)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			using (var mail = new SmtpClient())
				mail.Send(message);

			if (dispose)
				message.Dispose();
		}

		public static MailMessage AddHtml(this MailMessage message, string bodyHtml)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyHtml, null, MediaTypeNames.Text.Html));

			return message;
		}

		public static MailMessage AddPlain(this MailMessage message, string bodyPlain)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyPlain, null, MediaTypeNames.Text.Plain));

			return message;
		}

		// http://stackoverflow.com/a/9621399
		public static MemoryStream ToStream(this MailMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var assembly = typeof(SmtpClient).Assembly;
			var mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");

			var stream = new MemoryStream();

			var mailWriter = mailWriterType.CreateInstance(new[] { (object)stream, true });

			message.SetValue<object, object[]>("Send", new[] { mailWriter, true, true });

			mailWriter.SetValue<object, VoidType>("Close", null);

			return stream;
		}

		private static readonly Regex _emailRegex1 = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,10})+)$", RegexOptions.Compiled | RegexOptions.Singleline);

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
	}
}