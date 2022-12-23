namespace Ecng.Common
{
	using System.ComponentModel.DataAnnotations;

	/// <summary>
	/// Способ сравнения двух значений параметров.
	/// </summary>
	public enum ComparisonOperator
	{
		/// <summary>
		/// Значения параметров равны.
		/// </summary>
		[Display(Name = "=")]
		Equal,
		
		/// <summary>
		/// Значения параметров не равны.
		/// </summary>
		[Display(Name = "!=")]
		NotEqual,
		
		/// <summary>
		/// Значение левого параметра строго больше значения правого параметра.
		/// </summary>
		[Display(Name = ">")]
		Greater,
		
		/// <summary>
		/// Значение левого параметра нестрого больше (больше или равно) значения правого параметра.
		/// </summary>
		[Display(Name = ">=")]
		GreaterOrEqual,
		
		/// <summary>
		/// Значение левого параметра строго меньше значения правого параметра.
		/// </summary>
		[Display(Name = "<")]
		Less,
		
		/// <summary>
		/// Значение левого параметра нестрого меньше (меньше или равно) значения правого параметра.
		/// </summary>
		[Display(Name = "<=")]
		LessOrEqual,

		/// <summary>
		/// Значение левого параметра имеет любое значение.
		/// </summary>
		[Display(Name = "Any")]
		Any,

		/// <summary>
		/// Значение левого параметра содержится в правом.
		/// </summary>
		[Display(Name = "IN")]
		In,
	}
}
