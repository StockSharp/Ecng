using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecng.Common
{
	public class ConditionHelper
	{
		public static void CheckNotNullOrEmpty(string value, string name)
		{
			if(value.IsEmpty())
				throw new ArgumentNullException("name");
		}

		public static void CheckNotNull(object value, string name)
		{
			if (value == null)
				throw new ArgumentNullException("name");
		}
	}
}
