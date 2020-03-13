namespace Ecng.Interop
{
	using System;

	/// <summary>
	/// Features attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute : Attribute
	{
		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetPlatformAttribute"/>.
		/// </summary>
		/// <param name="platform">Platform.</param>
		public TargetPlatformAttribute(Platforms platform = Platforms.AnyCPU)
		{
			Platform = platform;
		}
	}
}