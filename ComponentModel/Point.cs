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
	[CommonTypeConverterNames("X,Y")]
	[Serializable]
	public class Point<T> : Equatable<Point<T>>
		where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
	{
		private static readonly IOperator<T> _operator = OperatorRegistry.GetOperator<T>();

		#region Point.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="Point{T}"/> class.
		/// </summary>
		public Point()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Point{T}"/> class.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		public Point(T x, T y)
		{
			X = x;
			Y = y;
		}

		#endregion

		/// <summary>
		/// Gets or sets the X.
		/// </summary>
		/// <value>The X.</value>
		public T X { get; set; }

		/// <summary>
		/// Gets or sets the Y.
		/// </summary>
		/// <value>The Y.</value>
		public T Y { get; set; }

		#region Object Members

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return "{{X={0}, Y={1}}}".Put(X, Y);
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return X.To<int>() ^ Y.To<int>();
		}

		#endregion

		#region Equatable<Point<T, TOperator>> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Point<T> other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y);
		}

		#endregion

		#region Cloneable<Point<T, TOperator>> Members

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Point<T> Clone()
		{
			return new Point<T>(X, Y);
		}

		#endregion

		/// <summary>
		/// Implements the operator +.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Point<T> operator +(Point<T> first, Point<T> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));

			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return new Point<T>(_operator.Add(first.X, second.X), _operator.Add(first.Y, second.Y));
		}

		/// <summary>
		/// Implements the operator -.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Point<T> operator -(Point<T> first, Point<T> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));

			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return new Point<T>(_operator.Subtract(first.X, second.X), _operator.Subtract(first.Y, second.Y));
		}

		/// <summary>
		/// Implements the operator *.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Point<T> operator *(Point<T> first, Point<T> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));

			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return new Point<T>(_operator.Multiply(first.X, second.X), _operator.Multiply(first.Y, second.Y));
		}

		/// <summary>
		/// Implements the operator /.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Point<T> operator /(Point<T> first, Point<T> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));

			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return new Point<T>(_operator.Divide(first.X, second.X), _operator.Divide(first.Y, second.Y));
		}

		/// <summary>
		/// Converts this instance.
		/// </summary>
		/// <typeparam name="TOther">The type of the other.</typeparam>
		/// <returns></returns>
		public Point<TOther> Convert<TOther>()
			where TOther : struct, IEquatable<TOther>
			//where TOtherOperator : IOperator<TOther>, new()
		{
			return new Point<TOther>(X.To<TOther>(), Y.To<TOther>());
		}
	}
}