namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// 
	/// </summary>
	public class Circle<T> : Equatable<Circle<T>>
		where T : struct, IEquatable<T>
	{
		private readonly static IOperator<T> _operator = OperatorRegistry.GetOperator<T>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		public Circle(Point<T> center, T radius)
		{
			if (center == null)
				throw new ArgumentNullException(nameof(center));

			if (_operator.Compare(radius, 0.To<T>()) < 0)
				throw new ArgumentOutOfRangeException(nameof(radius));

			Center = center;
			Radius = radius;
		}

		/// <summary>
		/// 
		/// </summary>
		public Point<T> Center { get; }

		/// <summary>
		/// 
		/// </summary>
		public T Radius { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public bool Contains(Point<T> point)
		{
			if (point == null)
				throw new ArgumentNullException(nameof(point));

			return _operator.Compare(new Line<T>(Center, point).Length.To<T>(), Radius) <= 0;
			//return new Line<T>(Center, point).Length <= Radius;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public T[] GetY(T x)
		{
			var sqrt = (_operator.Multiply(Radius, Radius).To<double>() - _operator.Subtract(x, Center.X).To<double>().Pow(2)).Sqrt();
			return new[] { _operator.Add(Center.Y, sqrt.To<T>()), _operator.Subtract(Center.Y, sqrt.To<T>()) };
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public Point<double>[] GetIntersection(Line<T> line)
		{
			if (line == null)
				throw new ArgumentNullException(nameof(line));

			//double
			//l = p2.X - p1.X,	// преобразуем отрезок в вид x = x1 + l * t, y = y1 + m * t
			//m = p2.Y - p1.Y,
			var x1c = Center.X.To<double>() - line.Start.X.To<double>();	// находим отрезок от цетра до начала нашего отрезка
			var y1c = Center.Y.To<double>() - line.Start.Y.To<double>();

			var a = line.DeltaX.To<double>() * line.DeltaX.To<double>() + line.DeltaY.To<double>() * line.DeltaY.To<double>();	// в уравнение окружности x2 + y2 = R2 подставляем x & y выше (с l & t) 
			var b = -2 * (line.DeltaX.To<double>() * x1c + line.DeltaY.To<double>() * y1c);
			var c = x1c * x1c + y1c * y1c - Radius.To<double>() * Radius.To<double>();

			//double d = b * b - 4 * a * c; // дескриминант квадратного уравнения
			//if (d < 0) // если он меньше нуля, то корней нет
			//    return null;

			//double
			//    ds = Math.Sqrt(d),
			//    t1 = (-b + ds) / (2 * a), // найденный t для пересечений
			//    t2 = (-b - ds) / (2 * a);

			var roots = MathHelper.GetRoots(a, b, c);
			if (roots.IsEmpty())
				return ArrayHelper.Empty<Point<double>>();

			var lp = new List<Point<double>>();
			if ((roots[0] >= 0) && (roots[0] <= 1))   // нас удовлетворяют только t в пределах [0..1] - отрезок
				lp.Add(new Point<double>(line.Start.X.To<double>() + line.DeltaX.To<double>() * roots[0], line.Start.Y.To<double>() + line.DeltaY.To<double>() * roots[0]));
			if ((roots[1] >= 0) && (roots[1] <= 1) && (roots[0] != roots[1]))
				lp.Add(new Point<double>(line.Start.X.To<double>() + line.DeltaX.To<double>() * roots[1], line.Start.Y.To<double>() + line.DeltaY.To<double>() * roots[1]));

			return lp.ToArray();
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Circle<T> Clone()
		{
			return new Circle<T>(Center, Radius);
		}

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Circle<T> other)
		{
			return Center == other.Center && _operator.Compare(Radius, other.Radius) == 0;
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Center.GetHashCode() ^ Radius.GetHashCode();
		}
	}
}