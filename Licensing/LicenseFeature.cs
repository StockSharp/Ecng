namespace Ecng.Licensing;

using System;

using Ecng.Common;

/// <summary>
/// License feature.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LicenseFeature"/>.
/// </remarks>
/// <param name="license"><see cref="License"/></param>
/// <param name="name"><see cref="Name"/></param>
/// <param name="expirationDate"><see cref="ExpirationDate"/></param>
/// <param name="expireAction"><see cref="ExpireAction"/></param>
/// <param name="hardwareId"><see cref="HardwareId"/></param>
/// <param name="account"><see cref="Account"/></param>
/// <param name="oneApp"><see cref="OneApp"/></param>
public class LicenseFeature(License license, string name, DateTime expirationDate, LicenseExpireActions expireAction, string hardwareId, string account, long? oneApp)
{
	/// <summary>
	/// License.
	/// </summary>
	public License License { get; } = license ?? throw new ArgumentNullException(nameof(license));

	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; } = name.ThrowIfEmpty(nameof(name));

	/// <summary>
	/// License expiry date.
	/// </summary>
	public DateTime ExpirationDate { get; } = expirationDate;

	/// <summary>
	/// Action when the license expired.
	/// </summary>
	public LicenseExpireActions ExpireAction { get; } = expireAction;

	/// <summary>
	/// Hardware id of the computer for which the license is issued.
	/// </summary>
	public string HardwareId { get; } = hardwareId;

	/// <summary>
	/// The account number for which the license is issued.
	/// </summary>
	public string Account { get; } = account;

	/// <summary>
	/// One app id.
	/// </summary>
	public long? OneApp { get; } = oneApp;

	/// <inheritdoc />
	public override string ToString() => $"{Name} (HDD={HardwareId} A={Account})";
}