namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;

	public  class GreaterThanZeroAttribute : ValidationAttribute
	{
		public override bool IsValid(object value)
		{
			try
			{
				return value.To<decimal?>() > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}