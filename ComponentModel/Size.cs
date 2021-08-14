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
	[CommonTypeConverterNames("Width,Height")]
	public class Size<T> : Equatable<Size<T>>
		where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
	{
		private static readonly IOperator<T> _operator = OperatorRegistry.GetOperator<T>();

		#region Size.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="Size{T}"/> class.
		/// </summary>
		public Size()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Size{T}"/> class.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public Size(T width, T height)
		{
			Width = width;
			Height = height;
		}

		#endregion

		/// <summary>
		/// Gets or sets the width.
		/// </summary>
		/// <value>The width.</value>
		public T Width { get; set; }

		/// <summary>
		/// Gets or sets the height.
		/// </summary>
		/// <value>The height.</value>
		public T Height { get; set; }

		/// <summary>
		/// Implements the operator +.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Size<T> operator +(Size<T> first, Size<T> second)
		{
			if (first is null)
				throw new ArgumentNullException(nameof(first));

			if (second is null)
				throw new ArgumentNullException(nameof(second));

			return new Size<T>(_operator.Add(first.Width, second.Width), _operator.Add(first.Height, second.Height));
		}

		/// <summary>
		/// Implements the operator -.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Size<T> operator -(Size<T> first, Size<T> second)
		{
			if (first is null)
				throw new ArgumentNullException(nameof(first));

			if (second is null)
				throw new ArgumentNullException(nameof(second));

			return new Size<T>(_operator.Subtract(first.Width, second.Width), _operator.Subtract(first.Height, second.Height));
		}

		/// <summary>
		/// Implements the operator *.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Size<T> operator *(Size<T> first, Size<T> second)
		{
			if (first is null)
				throw new ArgumentNullException(nameof(first));

			if (second is null)
				throw new ArgumentNullException(nameof(second));

			return new Size<T>(_operator.Multiply(first.Width, second.Width), _operator.Multiply(first.Height, second.Height));
		}

		/// <summary>
		/// Implements the operator /.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>The result of the operator.</returns>
		public static Size<T> operator /(Size<T> first, Size<T> second)
		{
			if (first is null)
				throw new ArgumentNullException(nameof(first));

			if (second is null)
				throw new ArgumentNullException(nameof(second));

			return new Size<T>(_operator.Divide(first.Width, second.Width), _operator.Divide(first.Height, second.Height));
		}

		//public static implicit operator System.Drawing.Size(Size s)
		//{
		//    return new System.Drawing.Size(s.Width, s.Height);
		//}

		//public static explicit operator Size(System.Drawing.Size s)
		//{
		//    return new Size(s.Width, s.Height);
		//}

		//public static Point<int> operator *(Point<int> pt, Size sz)
		//{
		//    return new Point<int>(pt.X * sz.Width, pt.Y * sz.Height);
		//}

		#region Equatable<Size<T>> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Size<T> other)
		{
			return Width.Equals(other.Width) && Height.Equals(other.Height);
		}

		#endregion

		#region Cloneable<Size<T>> Members

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Size<T> Clone()
		{
			return new Size<T>(Width, Height);
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
			return "{{Width={0}, Height={1}}}".Put(Width, Height);
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Width.To<int>() ^ Height.To<int>();
		}

		#endregion

		/// <summary>
		/// Converts this instance.
		/// </summary>
		/// <typeparam name="TOther">The type of the other.</typeparam>
		/// <returns></returns>
		public Size<TOther> Convert<TOther>()
			where TOther : struct, IEquatable<TOther>
		{
			return new Size<TOther>(Width.To<TOther>(), Height.To<TOther>());
		}
	}
}