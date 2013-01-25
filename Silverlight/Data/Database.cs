namespace Ecng.Data
{
	using System;
	using Ecng.Reflection;

	public class Database
	{
		public virtual T Read<T>(object id)
		{
			var retVal = Activator.CreateInstance<T>();
			retVal.SetValue("Id", (long)id);
			return retVal;
		}
	}
}
