namespace Ecng.Tests.Net;

using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;

using Ecng.Net;

[TestClass]
public class MailHelperTests : BaseTestClass
{
	private static readonly Regex _encodedWord = new(@"=\?([^?]+)\?([BQ])\?([^?]*)\?=", RegexOptions.IgnoreCase);

	private static (string charset, string encoding, string payload)[] ParseEncodedWords(string value)
	{
		var matches = _encodedWord.Matches(value);

		(matches.Count > 0).AssertTrue($"Expected RFC 2047 encoded-word(s), got: {value}");

		return [.. matches
			.Cast<Match>()
			.Select(m => (m.Groups[1].Value, m.Groups[2].Value.ToUpperInvariant(), m.Groups[3].Value))];
	}

	private static string DecodeEncodedWords(string value)
	{
		var sb = new StringBuilder();

		foreach (var (charset, encoding, payload) in ParseEncodedWords(value))
		{
			var charsetEncoding = Encoding.GetEncoding(charset);

			var decoded = encoding switch
			{
				"B" => charsetEncoding.GetString(Convert.FromBase64String(payload)),
				"Q" => DecodeQuotedPrintableWord(payload, charsetEncoding),
				_ => throw new NotSupportedException(encoding)
			};

			sb.Append(decoded);
		}

		return sb.ToString();
	}

	private static string DecodeQuotedPrintableWord(string payload, Encoding encoding)
	{
		var bytes = new List<byte>();

		for (var i = 0; i < payload.Length; i++)
		{
			var c = payload[i];

			if (c == '_')
			{
				bytes.Add((byte)' ');
				continue;
			}

			if (c == '=' && i + 2 < payload.Length)
			{
				bytes.Add(Convert.ToByte(payload.Substring(i + 1, 2), 16));
				i += 2;
				continue;
			}

			bytes.Add((byte)c);
		}

		return encoding.GetString([.. bytes]);
	}

	[TestMethod]
	public void AddHtmlAndPlain_AddsAlternateViews()
	{
		using var msg = new MailMessage();

		msg.AddHtml("<b>hello</b>");
		msg.AlternateViews.Count.AssertEqual(1);
		msg.AlternateViews[0].ContentType.MediaType.AssertEqual(MediaTypeNames.Text.Html);

		msg.AlternateViews.Clear();
		msg.AddPlain("plain text");
		msg.AlternateViews.Count.AssertEqual(1);
		msg.AlternateViews[0].ContentType.MediaType.AssertEqual(MediaTypeNames.Text.Plain);
	}

	[TestMethod]
	public void IsEmailValid_WorksForVariousAddresses()
	{
		"info@stocksharp.com".IsEmailValid().AssertTrue();
		"invalid@.com".IsEmailValid().AssertFalse();
		"plainaddress".IsEmailValid().AssertFalse();

		string email = null;
		ThrowsExactly<ArgumentNullException>(() => email.IsEmailValid());
	}

	[TestMethod]
	public void IsEmailValid_FormatVariants()
	{
		"user@example.com".IsEmailValid().AssertTrue();
		"USER@EXAMPLE.COM".IsEmailValid().AssertTrue();
		"first_last@example.com".IsEmailValid().AssertTrue();
		"user+tag@example.com".IsEmailValid().AssertTrue();

		// quoted local-part � accepted by MailAddress but rejected by regex
		"\"quoted@local\"@example.com".IsEmailValid().AssertFalse();

		// display name form is parsed by MailAddress but overall string doesn't match regex
		"John Doe <john@example.com>".IsEmailValid().AssertFalse();

		// domain as literal IP � MailAddress accepts but regex does not
		"user@[192.168.0.1]".IsEmailValid().AssertFalse();

		// multi-level domain should be OK
		"user@mail.example.co.uk".IsEmailValid().AssertTrue();

		// punycode domain (IDN) should be OK
		"user@xn--bcher-kva.ch".IsEmailValid().AssertTrue();

		// overly long TLD (>10) rejected by regex
		"user@example.averylongtldname".IsEmailValid().AssertFalse();
	}

	[TestMethod]
	[DataRow("user+tag@example.com")]
	[DataRow("first.last+tag@example.co.uk")]
	[DataRow("user+tag-123@xn--bcher-kva.ch")]
	public void IsEmailValid_AllowsPlusAddressing(string email)
	{
		email.IsEmailValid().AssertTrue();
	}

	[TestMethod]
	public void Attach_ToAttachment_CreateAttachment_Behavior()
	{
		var data = "hello world".UTF8();
		using var ms = new MemoryStream(data);

		// ToAttachment -> default Base64
		var att = MailHelper.ToAttachment("file.txt", new MemoryStream(data));
		att.AssertNotNull();
		att.NameEncoding.WebName.AssertEqual(Encoding.UTF8.WebName);
		att.TransferEncoding.AssertEqual(TransferEncoding.Base64);
		att.Name.AssertEqual("file.txt");

		// QuotedPrintable encoding
		using var ms2 = new MemoryStream(data);
		var attQ = MailHelper.CreateAttachment(ms2, "имя_файла_с_юникодом.txt", TransferEncoding.QuotedPrintable);
		attQ.AssertNotNull();
		attQ.TransferEncoding.AssertEqual(TransferEncoding.QuotedPrintable);
		attQ.NameEncoding.WebName.AssertEqual(Encoding.UTF8.WebName);
		attQ.Name.Contains("=?UTF-8?Q?").AssertTrue();

		// Empty filename shouldn't add attachment
		using var msg = new MailMessage();
		msg.Attach("", new MemoryStream(data));
		msg.Attachments.Count.AssertEqual(0);

		// Attach with non-empty name adds attachment and contains encoded name
		using var msg2 = new MailMessage();
		msg2.Attach("report.pdf", new MemoryStream(data));
		msg2.Attachments.Count.AssertEqual(1);
		msg2.Attachments[0].Name.Contains("report").AssertTrue();
	}

	[TestMethod]
	public void ToAttachment_EmptyFileName_Throws()
	{
		using var ms = new MemoryStream("x".UTF8());
		ThrowsExactly<ArgumentNullException>(() => MailHelper.ToAttachment("", ms));
	}

	[TestMethod]
	public void CreateAttachment_UnsupportedTransferEncoding_Throws()
	{
		using var ms = new MemoryStream("x".UTF8());
		// SevenBit is not supported by CreateAttachment switch -> should throw
		ThrowsExactly<ArgumentOutOfRangeException>(() => MailHelper.CreateAttachment(ms, "a.txt", TransferEncoding.SevenBit));
	}

	[TestMethod]
	public void CreateAttachment_SplitsLongNames()
	{
		// Create a long display name to force splitting into multiple encoded parts.
		var longName = new string('a', 100) + ".txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, longName, TransferEncoding.Base64);
		att.AssertNotNull();
		// Encoded name should contain multiple soft breaks ("?=") because of splitting.
		var occurrences = att.Name.Split(["?="], StringSplitOptions.None).Length - 1;
		(occurrences > 1).AssertTrue();
	}

	[TestMethod]
	public void CreateAttachment_Base64ChunksDoNotSplitUtf8Characters()
	{
		var displayName = new string('a', 11) + "я" + new string('b', 20) + ".txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, displayName, TransferEncoding.Base64);
		var strictUtf8 = new UTF8Encoding(false, true);

		foreach (var word in ParseEncodedWords(att.Name))
		{
			word.encoding.AssertEqual("B");
			strictUtf8.GetString(Convert.FromBase64String(word.payload));
		}
	}

	[TestMethod]
	public void CreateAttachment_Base64EncodedWordsAreWhitespaceSeparated()
	{
		var displayName = new string('a', 80) + ".txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, displayName, TransferEncoding.Base64);

		att.Name.Contains("?==?").AssertFalse($"Adjacent encoded-words require whitespace separator. Name: {att.Name}");
	}

	[TestMethod]
	public void CreateAttachment_QuotedPrintableEncodedWordsRoundTripNonAsciiName()
	{
		const string displayName = "имя файла.txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, displayName, TransferEncoding.QuotedPrintable);

		DecodeEncodedWords(att.Name).AssertEqual(displayName);
	}

	[TestMethod]
	public void CreateAttachment_QuotedPrintableDoesNotEmitRawSpacesInsideEncodedWords()
	{
		const string displayName = "file name with spaces.txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, displayName, TransferEncoding.QuotedPrintable);

		foreach (var word in ParseEncodedWords(att.Name))
			word.payload.Contains(' ').AssertFalse($"Encoded-word payload must encode spaces as underscores. Name: {att.Name}");
	}

	[TestMethod]
	public void Attach_NullMessage_Throws()
	{
		MailMessage msg = null;
		using var ms = new MemoryStream("x".UTF8());
		ThrowsExactly<ArgumentNullException>(() => MailHelper.Attach(msg, "file.txt", ms));
	}
}
