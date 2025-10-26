namespace Ecng.Tests.Net;

using System.Net.Mail;
using System.Net.Mime;
using System.Text;

using Ecng.Net;

[TestClass]
public class MailHelperTests
{
	[TestMethod]
	public Task Send_Null_Throws()
	{
		MailMessage msg = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => msg.Send());
		return Assert.ThrowsExactlyAsync<ArgumentNullException>(() => msg.SendAsync(CancellationToken.None));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => email.IsEmailValid());
	}

	[TestMethod]
	public void IsEmailValid_FormatVariants()
	{
		// These expectations reflect current implementation: it uses MailAddress plus an additional regex
		// which does not allow '+' or some quoted/local forms, nor display-name wrappers.

		"user@example.com".IsEmailValid().AssertTrue();
		"USER@EXAMPLE.COM".IsEmailValid().AssertTrue();
		"first_last@example.com".IsEmailValid().AssertTrue();

		// '+' in local part is accepted by MailAddress but disallowed by the regex used here
		"user+tag@example.com".IsEmailValid().AssertFalse();

		// quoted local-part Ч accepted by MailAddress but rejected by regex
		"\"quoted@local\"@example.com".IsEmailValid().AssertFalse();

		// display name form is parsed by MailAddress but overall string doesn't match regex
		"John Doe <john@example.com>".IsEmailValid().AssertFalse();

		// domain as literal IP Ч MailAddress accepts but regex does not
		"user@[192.168.0.1]".IsEmailValid().AssertFalse();

		// multi-level domain should be OK
		"user@mail.example.co.uk".IsEmailValid().AssertTrue();

		// punycode domain (IDN) should be OK
		"user@xn--bcher-kva.ch".IsEmailValid().AssertTrue();

		// overly long TLD (>10) rejected by regex
		"user@example.averylongtldname".IsEmailValid().AssertFalse();
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
		att.Name.Contains("=?UTF-8?B?").AssertTrue();

		// QuotedPrintable encoding
		using var ms2 = new MemoryStream(data);
		var attQ = MailHelper.CreateAttachment(ms2, "им€_файла_с_юникодом.txt", System.Net.Mime.TransferEncoding.QuotedPrintable);
		attQ.AssertNotNull();
		attQ.TransferEncoding.AssertEqual(TransferEncoding.QuotedPrintable);
		attQ.NameEncoding.WebName.AssertEqual(Encoding.GetEncoding("ISO-8859-1").WebName);
		attQ.Name.Contains("=?ISO-8859-1?Q?").AssertTrue();

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
		Assert.ThrowsExactly<ArgumentNullException>(() => MailHelper.ToAttachment("", ms));
	}

	[TestMethod]
	public void CreateAttachment_UnsupportedTransferEncoding_Throws()
	{
		using var ms = new MemoryStream("x".UTF8());
		// SevenBit is not supported by CreateAttachment switch -> should throw
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MailHelper.CreateAttachment(ms, "a.txt", System.Net.Mime.TransferEncoding.SevenBit));
	}

	[TestMethod]
	public void CreateAttachment_SplitsLongNames()
	{
		// Create a long display name to force splitting into multiple encoded parts.
		var longName = new string('a', 100) + ".txt";
		using var ms = new MemoryStream("x".UTF8());
		var att = MailHelper.CreateAttachment(ms, longName, System.Net.Mime.TransferEncoding.Base64);
		att.AssertNotNull();
		// Encoded name should contain multiple soft breaks ("?=") because of splitting.
		var occurrences = att.Name.Split(["?="], StringSplitOptions.None).Length - 1;
		(occurrences > 1).AssertTrue();
	}

	[TestMethod]
	public void Attach_NullMessage_Throws()
	{
		MailMessage msg = null;
		using var ms = new MemoryStream("x".UTF8());
		Assert.ThrowsExactly<ArgumentNullException>(() => MailHelper.Attach(msg, "file.txt", ms));
	}
}