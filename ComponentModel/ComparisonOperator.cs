namespace Ecng.ComponentModel
{
	/// <summary>
	/// Способ сравнения двух значений параметров.
	/// </summary>
	public enum ComparisonOperator
	{
		/// <summary>
		/// Значения параметров равны.
		/// </summary>
		[EnumDisplayName("Equal", true)]
		Equal,
		
		/// <summary>
		/// Значения параметров не равны.
		/// </summary>
		[EnumDisplayName("Not equal", true)]
		NotEqual,
		
		/// <summary>
		/// Значение левого параметра строго больше значения правого параметра.
		/// </summary>
		[EnumDisplayName("Greater", true)]
		Greater,
		
		/// <summary>
		/// Значение левого параметра нестрого больше (больше или равно) значения правого параметра.
		/// </summary>
		[EnumDisplayName("Greater or equal", true)]
		GreaterOrEqual,
		
		/// <summary>
		/// Значение левого параметра строго меньше значения правого параметра.
		/// </summary>
		[EnumDisplayName("Less", true)]
		Less,
		
		/// <summary>
		/// Значение левого параметра нестрого меньше (меньше или равно) значения правого параметра.
		/// </summary>
		[EnumDisplayName("Less or equal", true)]
		LessOrEqual,

		/// <summary>
		/// Значение левого параметра имеет любое значение.
		/// </summary>
		[EnumDisplayName("Any", true)]
		Any,
	}
}
