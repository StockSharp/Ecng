namespace Ecng.Common
{
	using System;

	/// <summary>
	/// Features attribute.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="TargetPlatformAttribute"/>.
	/// </remarks>
	/// <param name="platform">Platform.</param>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute(Platforms platform = Platforms.AnyCPU) : Attribute
	{
		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; } = platform;
	}
}