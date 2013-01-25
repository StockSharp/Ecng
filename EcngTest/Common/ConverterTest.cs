namespace Ecng.Test.Common
{
	using System;
	using System.Collections.ObjectModel;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ConverterTest
	{
		[TestMethod]
		public void Type2String()
		{
			typeof(ObservableCollection<int>).To<string>().To<Type>().AssertEqual(typeof(ObservableCollection<int>));
			typeof(int).To<string>().To<Type>().AssertEqual(typeof(int));
			"int".To<Type>().AssertEqual(typeof(int));
		}
	}
}