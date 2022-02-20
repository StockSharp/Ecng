namespace Ecng.Drawing
{
	/// <summary>
	/// Thickness.
	/// </summary>
	public struct Thickness
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Thickness"/>.
		/// </summary>
		/// <param name="left"><see cref="Left"/>.</param>
		/// <param name="top"><see cref="Top"/>.</param>
		/// <param name="right"><see cref="Right"/>.</param>
		/// <param name="bottom"><see cref="Bottom"/>.</param>
		public Thickness(double left, double top, double right, double bottom) : this()
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		/// <summary>
		/// Gets or sets the width, in pixels, of the lower side of the bounding rectangle.
		/// </summary>
		public double Bottom { get; set; }

		/// <summary>
		/// Gets or sets the width, in pixels, of the left side of the bounding rectangle.
		/// </summary>
		public double Left { get; set; }

		/// <summary>
		/// Gets or sets the width, in pixels, of the right side of the bounding rectangle.
		/// </summary>
		public double Right { get; set; }

		/// <summary>
		/// Gets or sets the width, in pixels, of the upper side of the bounding rectangle.
		/// </summary>
		public double Top { get; set; }
	}
}