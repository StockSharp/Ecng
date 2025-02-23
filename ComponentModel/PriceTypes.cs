namespace Ecng.ComponentModel;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Measure units.
/// </summary>
[Serializable]
[DataContract]
public enum PriceTypes
{
	/// <summary>
	/// The absolute value. Incremental change is a given number.
	/// </summary>
	[EnumMember]
	Absolute,

	/// <summary>
	/// Percents. Step change - one hundredth of a percent.
	/// </summary>
	[EnumMember]
	Percent,

	/// <summary>
	/// The limited value. This unit allows to set a specific change number, which cannot be used in arithmetic operations <see cref="Price"/>.
	/// </summary>
	[EnumMember]
	Limit,
}