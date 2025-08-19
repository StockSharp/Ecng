namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using Ecng.Localization;

/// <summary>
/// Actions when a license expired.
/// </summary>
[DataContract]
[Serializable]
public enum LicenseExpireActions
{
	/// <summary>
	/// Prevent work.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PreventWorkKey)]
	PreventWork,

	/// <summary>
	/// Prevent upgrade.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PreventUpgradeKey)]
	PreventUpgrade,
}