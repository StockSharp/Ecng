namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// 
	/// </summary>
	public class Line<T> : Equatable<Line<T>>
		where T : struct, IEquatable<T>
	{
		private readonly static IOperator<T> _operator = OperatorRegistry.GetOperator<T>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public Line(Point<T> start, Point<T> end)
		{
			if (start == null)
				throw new ArgumentNullException(nameof(start));

			if (end == null)
				throw new ArgumentNullException(nameof(end));

			Start = start;
			End = end;

			DeltaX = _operator.Subtract(end.X, start.X);
			DeltaY = _operator.Subtract(end.Y, start.Y);
			Length = _operator.Add(_operator.Multiply(DeltaX, DeltaX), _operator.Multiply(DeltaY, DeltaY)).To<double>().Sqrt();
			Angle = GetAngle();
		}

		/// <summary>
		/// 
		/// </summary>
		public Point<T> Start { get; }

		/// <summary>
		/// 
		/// </summary>
		public Point<T> End { get; }

		/// <summary>
		/// 
		/// </summary>
		public double Length { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public T DeltaX { get; }

		/// <summary>
		/// 
		/// </summary>
		public T DeltaY { get; }

		/// <summary>
		/// 
		/// </summary>
		public double Angle { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public T GetY(T x)
		{
			return _operator.Add(_operator.Divide(_operator.Multiply(_operator.Subtract(x, Start.X), DeltaY), DeltaX), Start.Y);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private double GetAngle()
		{
			//if (start == null)
			//    throw new ArgumentNullException("start");

			//if (End == null)
			//    throw new ArgumentNullException("End");

			if (Start.Equals(End))
				return 0;

			double angle;

			if (_operator.Compare(Start.X, End.X) != 0 && _operator.Compare(Start.Y, End.Y) != 0)
			{
				var deltaX = DeltaX.To<double>();
				var deltaY = DeltaY.To<double>();
				angle = ((deltaY / deltaX).Atan() * 180) / Math.PI;

				if (deltaX < 0 && deltaY > 0)
					angle += 180;
				else if (deltaX < 0 && deltaY < 0)
					angle += 180;
				else if (deltaX > 0 && deltaY < 0)
					angle += 360;

				angle = 360 - angle;
			}
			else
			{
				if (_operator.Compare(Start.X, End.X) == 0)
				{
					angle = _operator.Compare(Start.Y, End.Y) < 0 ? 270 : 90;
				}
				else
				{
					angle = _operator.Compare(Start.X, End.X) < 0 ? 0 : 180;
				}
			}

			if (angle < 0)
				throw new InvalidOperationException("Angle must positive. Angle = '{0}'.".Put(angle));

			return angle;
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Line<T> Clone()
		{
			return new Line<T>(Start, End);
		}

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Line<T> other)
		{
			return Start == other.Start && End == other.End;
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Start.GetHashCode() ^ End.GetHashCode();
		}
	}
}