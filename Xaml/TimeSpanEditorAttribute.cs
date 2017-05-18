namespace Ecng.Xaml
{
	using System;

	/// <summary>
	/// Show <see cref="TimeSpan"/> editor parts mask.
	/// </summary>
	[Flags]
	public enum TimeSpanEditorMask
	{
		/// <summary>
		/// Days.
		/// </summary>
		Days = 1,

		/// <summary>
		/// Hours.
		/// </summary>
		Hours = 2,

		/// <summary>
		/// Minutes.
		/// </summary>
		Minutes = 4,

		/// <summary>
		/// Seconds.
		/// </summary>
		Seconds = 8,

		/// <summary>
		/// Milliseconds.
		/// </summary>
		Milliseconds = 16,

		/// <summary>
		/// Microseconds.
		/// </summary>
		Microseconds = 32,
	}

	/// <summary>
	/// <see cref="TimeSpan"/> editor attribute.
	/// </summary>
	public class TimeSpanEditorAttribute : Attribute
	{
		/// <summary>
		/// Show editor parts mask.
		/// </summary>
		public TimeSpanEditorMask Mask { get; set; } = TimeSpanEditor.DefaultMask;
	}
}