namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class Range<T> : Equatable<Range<T>>
		where T : IComparable<T>
	{
		/// <summary>
		/// Initializes the <see cref="Range{T}"/> class.
		/// </summary>
		static Range()
		{
			MinValue = default(T);
			MaxValue = default(T);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Range{T}"/> class.
		/// </summary>
		public Range()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Range{T}"/> class.
		/// </summary>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		public Range(T min, T max)
		{
			Init(min, max);
		}

		/// <summary>
		/// Represents the smallest possible value of a T.
		/// </summary>
		public static readonly T MinValue;

		/// <summary>
		/// Represents the largest possible value of a T.
		/// </summary>
		public static readonly T MaxValue;

		#region HasMinValue

		/// <summary>
		/// Gets a value indicating whether this instance has min value.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has min value; otherwise, <c>false</c>.
		/// </value>
		public bool HasMinValue => _min.HasValue;

		#endregion

		#region HasMaxValue

		/// <summary>
		/// Gets a value indicating whether this instance has max value.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has max value; otherwise, <c>false</c>.
		/// </value>
		public bool HasMaxValue => _max.HasValue;

		#endregion

		#region Length

		private static IOperator<T> _operator;

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public T Length
		{
			get
			{
				if (!HasMinValue || !HasMaxValue)
					return MaxValue;
				else
				{
					if (_operator == null)
						_operator = OperatorRegistry.GetOperator<T>();

					return _operator.Subtract(Max, Min);
				}
			}
		}

		#endregion

		#region Min

		//private bool _isMinInit;
		private readonly NullableEx<T> _min = new NullableEx<T>();

		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		/// <value>The min value.</value>
		[Field("Min", Order = 0)]
		public T Min
		{
			get { return _min.Value; }
			set
			{
				if (_max.HasValue)
					ValidateBounds(value, Max);

				_min.Value = value;
				//_isMinInit = true;
			}
		}

		#endregion

		#region Max

		//private bool _isMaxInit;
		private readonly NullableEx<T> _max = new NullableEx<T>();

		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		/// <value>The max value.</value>
		[Field("Max", Order = 1)]
		public T Max
		{
			get { return _max.Value; }
			set
			{
				if (_min.HasValue)
					ValidateBounds(Min, value);

				_max.Value = value;
				//_isMaxInit = true;
			}
		}

		#endregion

		#region Parse

		/// <summary>
		/// Parses the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static Range<T> Parse(string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			if (value.Length < 3)
				throw new ArgumentOutOfRangeException(nameof(value));

			value = value.Substring(1, value.Length - 2);
			value = value.Replace("Min:", string.Empty).Replace("Max:", string.Empty);
			var parts = value.Split(' ');

			return new Range<T>(parts[0].To<T>(), parts[1].To<T>());
		}

		#endregion

		#region Object Members

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Min.GetHashCode() ^ Max.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return "{{Min:{0} Max:{1}}}".Put(HasMinValue ? Min.ToString() : "null", HasMaxValue ? Max.ToString() : "null");
		}

		#endregion

		#region Equatable<Range<T>> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The value.</param>
		/// <returns></returns>
		protected override bool OnEquals(Range<T> other)
		{
			return _min == other._min && _max == other._max;
		}

		#endregion

		//#region Serializable<Range<T>> Members

		///// <summary>
		///// Serializes object into specified source.
		///// </summary>
		///// <param name="serializer"></param>
		///// <param name="fields"></param>
		///// <param name="source">Serialized state.</param>
		//protected override void Serialize(ISerializer serializer, FieldCollection fields, SerializationItemCollection source)
		//{
		//    if (HasMinValue)
		//        source.Add(new SerializationItem(fields["Min"], Min.To(BaseType)));

		//    if (HasMaxValue)
		//        source.Add(new SerializationItem(fields["Max"], Max.To(BaseType)));
		//}

		///// <summary>
		///// Deserialize object into specified source.
		///// </summary>
		///// <param name="serializer"></param>
		///// <param name="fields"></param>
		///// <param name="source">Serialized state.</param>
		//protected override void Deserialize(ISerializer serializer, FieldCollection fields, SerializationItemCollection source)
		//{
		//    var min = source["Min"];
		//    if (min != null && min.Value != null)
		//        Min = min.Value.To<T>();

		//    var max = source["Max"];
		//    if (max != null && max.Value != null)
		//        Max = max.Value.To<T>();
		//}

		//#endregion

		public override Range<T> Clone()
		{
			return new Range<T>(Min, Max);
		}

		public bool Contains(Range<T> range)
		{
			if (range == null)
				throw new ArgumentNullException(nameof(range));

			return Contains(range.Min) && Contains(range.Max);
		}

		public Range<T> Intersect(Range<T> range)
		{
			if (range == null)
				throw new ArgumentNullException(nameof(range));

			if (Contains(range))
				return range.Clone();
			else if (range.Contains(this))
				return Clone();
			else
			{
				var containsMin = Contains(range.Min);
				var containsMax = Contains(range.Max);

				if (containsMin)
					return new Range<T>(range.Min, Max);
				else if (containsMax)
					return new Range<T>(Min, range.Max);
				else
					return null;
			}
		}

		public Range<T> SubRange(T min, T max)
		{
			if (!Contains(min))
				throw new ArgumentException("min");

			if (!Contains(max))
				throw new ArgumentException("max");

			return new Range<T>(min, max);
		}

		/// <summary>
		/// Determines whether [contains] [the specified value].
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>
		/// 	<c>true</c> if [contains] [the specified value]; otherwise, <c>false</c>.
		/// </returns>
		public bool Contains(T value)
		{
			if (_min.HasValue && Min.CompareTo(value) > 0)
				return false;
			else if (_max.HasValue && Max.CompareTo(value) < 0)
				return false;
			else
				return true;
		}

		private void Init(T min, T max)
		{
			ValidateBounds(min, max);

			_min.Value = min;
			_max.Value = max;
		}

		private static void ValidateBounds(T min, T max)
		{
			if (min.CompareTo(max) > 0)
				throw new ArgumentOutOfRangeException(nameof(min), "Min value {0} is more than max value {1}.".Put(min, max));
		}
	}
}