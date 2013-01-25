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
		[EnumDisplayName("Равно")]
		Equal,
		
		/// <summary>
		/// Значения параметров не равны.
		/// </summary>
		[EnumDisplayName("Не равно")]
		NotEqual,
		
		/// <summary>
		/// Значение левого параметра строго больше значения правого параметра.
		/// </summary>
		[EnumDisplayName("Больше")]
		Greater,
		
		/// <summary>
		/// Значение левого параметра нестрого больше (больше или равно) значения правого параметра.
		/// </summary>
		[EnumDisplayName("Больше или равно")]
		GreaterOrEqual,
		
		/// <summary>
		/// Значение левого параметра строго меньше значения правого параметра.
		/// </summary>
		[EnumDisplayName("Меньше")]
		Less,
		
		/// <summary>
		/// Значение левого параметра нестрого меньше (меньше или равно) значения правого параметра.
		/// </summary>
		[EnumDisplayName("Меньше или равно")]
		LessOrEqual,

		/// <summary>
		/// Значение левого параметра имеет любое значение.
		/// </summary>
		[EnumDisplayName("Любое")]
		Any,
	}
}
