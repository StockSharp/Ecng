namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.ComponentModel;

	using Ecng.Common;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[TypeConverter(typeof(CommonTypeConverter))]
	[CommonTypeConverterNames("Location,Size")]
	[Serializable]
	public class Rectangle<T> : Equatable<Rectangle<T>>
		where T : struct, IEquatable<T>
	{
		#region Private Fields

		//private readonly Range<T> _xRange = new Range<T>();
		//private readonly Range<T> _yRange = new Range<T>();

		private readonly static IOperator<T> _operator = OperatorRegistry.GetOperator<T>();

		#endregion

		#region Rectangle.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="Rectangle{T}"/> class.
		/// </summary>
		public Rectangle()
		{
			Location = new Point<T>();
			Size = new Size<T>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rectangle{T}"/> class.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="top">The top.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public Rectangle(T left, T top, T width, T height)
			: this(new Point<T>(left, top), new Size<T>(width, height))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rectangle{T}"/> class.
		/// </summary>
		/// <param name="leftTop">The left top location.</param>
		/// <param name="rightBottom">The right bottom size.</param>
		public Rectangle(Point<T> leftTop, Point<T> rightBottom)
		{
			if (leftTop is null)
				throw new ArgumentNullException(nameof(leftTop));

			if (rightBottom is null)
				throw new ArgumentNullException(nameof(rightBottom));

			if (_operator.Compare(rightBottom.X, leftTop.X) < 0)
				throw new ArgumentException(nameof(rightBottom));

			if (_operator.Compare(rightBottom.Y, leftTop.Y) < 0)
				throw new ArgumentException(nameof(rightBottom));

			Location = leftTop;
			Size = new Size<T>(_operator.Subtract(rightBottom.X, leftTop.X), _operator.Subtract(rightBottom.Y, leftTop.Y));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rectangle{T}"/> class.
		/// </summary>
		/// <param name="location">The location.</param>
		/// <param name="size">The size.</param>
		public Rectangle(Point<T> location, Size<T> size)
		{
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Size = size ?? throw new ArgumentNullException(nameof(size));
		}

		#endregion

		#region Center

		/// <summary>
		/// Gets the center.
		/// </summary>
		/// <value>The center.</value>
		public Point<T> Center => new Point<T>(_operator.Add(Location.X, _operator.Divide(Size.Width, 2.To<T>())), _operator.Add(Location.Y, _operator.Divide(Size.Height, 2.To<T>())));

		#endregion

		#region Right

		/// <summary>
		/// Gets the right.
		/// </summary>
		/// <value>The right.</value>
		public T Right => _operator.Add(Location.X, Size.Width);

		#endregion

		#region Bottom

		/// <summary>
		/// Gets the bottom.
		/// </summary>
		/// <value>The bottom.</value>
		public T Bottom => _operator.Add(Location.Y, Size.Height);

		#endregion

		#region Location

		/// <summary>
		/// Gets or sets the location.
		/// </summary>
		/// <value>The location.</value>
		public Point<T> Location { get; }

		#endregion

		#region Size

		/// <summary>
		/// Gets or sets the size.
		/// </summary>
		/// <value>The size.</value>
		public Size<T> Size { get; }

		#endregion

		#region Contains

		/// <summary>
		/// Determines whether [contains] [the specified x].
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns>
		/// 	<c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(T x, T y)
		{
			return
					_operator.Compare(x, Location.X) >= 0 &&
					_operator.Compare(x, Right) <= 0 &&
					_operator.Compare(y, Location.Y) >= 0 &&
					_operator.Compare(y, Bottom) <= 0;
		}

		/// <summary>
		/// Determines whether [contains] [the specified point].
		/// </summary>
		/// <param name="point">The point.</param>
		/// <returns>
		/// 	<c>true</c> if [contains] [the specified point]; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(Point<T> point)
		{
			if (point is null)
				throw new ArgumentNullException(nameof(point));

			return Contains(point.X, point.Y);
		}

		//public bool Contains(Rectangle<T> rect)
		//{
		//    if (((Location.X <= rect.Location.X) && ((rect.Location.X + rect.Size.Width) <= (Location.X + Size.Width))) && (Location.Y <= rect.Location.Y))
		//        return ((rect.Location.Y + rect.Size.Height) <= (Location.Y + Size.Height));

		//    return false;
		//}

		#endregion

		#region Equatable<Rectangle<T, TOperator>> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Rectangle<T> other)
		{
			return Location.Equals(other.Location) && Size.Equals(other.Size);
		}

		#endregion

		#region Cloneable<Size<T, TOperator>> Members

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Rectangle<T> Clone()
		{
			return new Rectangle<T>(Location, Size);
		}

		#endregion

		#region Object Members

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return "{{Location={0}, Size={1}}}".Put(Location, Size);
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Location.GetHashCode() ^ Size.GetHashCode();
		}

		#endregion

		/// <summary>
		/// Converts this instance.
		/// </summary>
		/// <typeparam name="TOther">The type of the other.</typeparam>
		/// <returns></returns>
		public Rectangle<TOther> Convert<TOther>()
			where TOther : struct, IEquatable<TOther>
		{
			return new Rectangle<TOther>(Location.Convert<TOther>(), Size.Convert<TOther>());
		}
	}
}