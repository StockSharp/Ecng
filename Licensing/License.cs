namespace Ecng.Licensing;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Ecng.Common;
using Ecng.Collections;

/// <summary>
/// License.
/// </summary>
public class License
{
	private const string _dateFormat = "yyyyMMdd HH:mm:ss";

	/// <summary>
	/// Initializes a new instance of the <see cref="License"/>.
	/// </summary>
	/// <param name="body">License body.</param>
	public License(byte[] body)
	{
		Body = body ?? throw new ArgumentNullException(nameof(body));

		var xml = body.UTF8().To<XElement>();

		Version = xml.GetElementValue("ver", new Version(1, 0));
		Id = xml.GetElementValue<long>("id");
		IssuedTo = xml.GetElementValue<string>("issuedTo");

		var issuedDateStr = xml.GetElementValue<string>("issuedDate");

		IssuedDate = (long.TryParse(issuedDateStr, out var issuedDate) ? issuedDate.To<DateTime>() : issuedDateStr.ToDateTime(_dateFormat)).UtcKind();

		var platformsElem = xml.Element("platforms");

		if (platformsElem?.HasElements == true)
		{
			foreach (var platformElem in platformsElem.Elements("platform"))
			{
				Features.Add(platformElem.GetAttributeValue<string>("name").To<OSPlatform>(),
					[.. platformElem.Elements("feature").Select(featureElem => new LicenseFeature(this,
						featureElem.GetAttributeValue<string>("name"),
						featureElem.GetAttributeValue<long>("expire").To<DateTime>().UtcKind(),
						featureElem.GetAttributeValue<LicenseExpireActions>("expireAction"),
						featureElem.GetAttributeValue<string>("hardwareId"),
						featureElem.GetAttributeValue<string>("account"),
						featureElem.GetAttributeValue<long?>("oneApp")))]);
			}
		}
		else
		{
			var expirationDateStr = xml.GetElementValue<string>("expirationDate");

			var expDate = (long.TryParse(expirationDateStr, out var expirationDate) ? expirationDate.To<DateTime>() : expirationDateStr.ToDateTime(_dateFormat)).UtcKind();
			var hddId = xml.GetElementValue("hardwareId", string.Empty);
			var account = xml.GetElementValue("account", string.Empty);
			var expAction = xml.GetElementValue("expireAction", LicenseExpireActions.PreventWork);

			var platformsStr = platformsElem?.Value;

			var platforms = platformsStr.IsEmpty()
				? [OSPlatform.Windows]
				: platformsStr.SplitByComma().Select(p => p == "Windows" ? OSPlatform.Windows : p.To<OSPlatform>()).ToArray();

			var featuresStr = xml.GetElementValue<string>("features").SplitByComma();

			foreach (var platform in platforms)
			{
				Features.TryAdd2(platform, [.. featuresStr.Select(feature => new LicenseFeature(this, feature, expDate, expAction, hddId, account, null))]);
			}
		}

		Signature = xml.GetElementValue<string>("signature").Base64();

		xml.Element("signature").Remove();

		var bodyWithoutSignature = xml.To<string>();

		if (!OSPlatform.Windows.IsOSPlatform())
		{
			bodyWithoutSignature = bodyWithoutSignature.Replace(Environment.NewLine, "\r\n");
		}

		BodyWithoutSignature = bodyWithoutSignature.UTF8();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="License"/>.
	/// </summary>
	/// <param name="fileName"><see cref="FileName"/></param>
	/// <param name="body">License body.</param>
	public License(string fileName, byte[] body)
		: this(body)
	{
		FileName = fileName;
	}

	/// <summary>
	/// License file name.
	/// </summary>
	public string FileName { get; }

	/// <summary>
	/// File format version.
	/// </summary>
	public Version Version { get; }

	/// <summary>
	/// License id.
	/// </summary>
	public long Id { get; }

	/// <summary>
	/// Email.
	/// </summary>
	public string IssuedTo { get; }

	/// <summary>
	/// Date of licensing.
	/// </summary>
	public DateTime IssuedDate { get; }

	/// <summary>
	/// Possible feature list.
	/// </summary>
	public IDictionary<OSPlatform, IEnumerable<LicenseFeature>> Features { get; } = new Dictionary<OSPlatform, IEnumerable<LicenseFeature>>();

	/// <summary>
	/// License body.
	/// </summary>
	public byte[] Body { get; }

	/// <summary>
	/// Body with no signature.
	/// </summary>
	public byte[] BodyWithoutSignature { get; }

	/// <summary>
	/// Signature.
	/// </summary>
	public byte[] Signature { get; }

	/// <inheritdoc />
	public override string ToString()
		=> $"N{Id} ({Features.Values.FirstOrDefault()?.FirstOrDefault(f => !f.HardwareId.IsEmpty())?.HardwareId})";
}