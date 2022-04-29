namespace Ecng.Tests.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.UnitTesting;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ReflectionTests
	{
		[TestMethod]
		public void ItemType()
		{
			var arr = new[] { 1, 2, 3 };
			var list = new List<int> { 1, 2, 3 };
			var enu = (IEnumerable<int>)arr;
			arr.GetType().GetItemType().AssertSame(typeof(int));
			list.GetType().GetItemType().AssertSame(typeof(int));
			enu.GetType().GetItemType().AssertSame(typeof(int));
			typeof(IAsyncEnumerable<int>).GetItemType().AssertSame(typeof(int));
		}

		[TestMethod]
		public void ToStorage()
		{
			var type = typeof(Disposable);
			var prop = type.GetProperty(nameof(Disposable.IsDisposed));
			var method = type.GetMethod(nameof(Disposable.Dispose));

			void Do(bool isAssemblyQualifiedName)
			{
				type.ToStorage(isAssemblyQualifiedName).ToMember<Type>().AssertEqual(type);
				prop.ToStorage(isAssemblyQualifiedName).ToMember<PropertyInfo>().AssertEqual(prop);
				method.ToStorage(isAssemblyQualifiedName).ToMember<MethodInfo>().AssertEqual(method);

				type.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(type);
				prop.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(prop);
				method.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(method);
			}

			Do(true);
			Do(false);
		}
	}
}