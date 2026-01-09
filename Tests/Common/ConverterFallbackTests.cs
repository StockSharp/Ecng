namespace Ecng.Tests.Common;

// Tests for Converter fallback functionality must not run in parallel
// because they modify static TypeFallback and EnumFallback properties
[TestClass]
[DoNotParallelize]
public class ConverterFallbackTests : BaseTestClass
{
	[TestMethod]
	public void TypeFallback_Success()
	{
		var prevFallback = Converter.TypeFallback;
		try
		{
			// Use unique type name to avoid cache conflicts with other tests
			var typeName = $"TestType_{Guid.NewGuid():N}, TestAssembly";

			Converter.TypeFallback = input =>
			{
				// Simulate migration: OldNamespace.OldType -> NewType
				if (input.Contains("TestType_"))
					return typeof(string);

				return null;
			};

			// Should use fallback and return string type
			typeName.To<Type>().AssertEqual(typeof(string));
		}
		finally
		{
			Converter.TypeFallback = prevFallback;
		}
	}

	[TestMethod]
	public void TypeFallback_ThrowsWhenReturnsNull()
	{
		var prevFallback = Converter.TypeFallback;
		try
		{
			// Use unique type name to avoid cache conflicts
			var typeName = $"FallbackNull_{Guid.NewGuid():N}, TestAssembly";

			Converter.TypeFallback = input => null;

			ThrowsExactly<InvalidCastException>(() => typeName.To<Type>());
		}
		finally
		{
			Converter.TypeFallback = prevFallback;
		}
	}

	[TestMethod]
	public void TypeFallback_ThrowsWhenNotSet()
	{
		var prevFallback = Converter.TypeFallback;
		try
		{
			// Use unique type name to avoid cache conflicts
			var typeName = $"FallbackNotSet_{Guid.NewGuid():N}, TestAssembly";

			Converter.TypeFallback = null;

			ThrowsExactly<InvalidCastException>(() => typeName.To<Type>());
		}
		finally
		{
			Converter.TypeFallback = prevFallback;
		}
	}

	private enum TestMigrationEnum
	{
		Value1,
		Value2,
		NewValue
	}

	[TestMethod]
	public void EnumFallback_Success()
	{
		var prevFallback = Converter.EnumFallback;
		try
		{
			Converter.EnumFallback = (enumType, value) =>
			{
				// Simulate migration: OldValue -> NewValue
				if (enumType == typeof(TestMigrationEnum) && value == "OldValue")
					return TestMigrationEnum.NewValue;

				return null;
			};

			// Should use fallback and return NewValue
			"OldValue".To<TestMigrationEnum>().AssertEqual(TestMigrationEnum.NewValue);

			// Normal values should still work
			"Value1".To<TestMigrationEnum>().AssertEqual(TestMigrationEnum.Value1);
		}
		finally
		{
			Converter.EnumFallback = prevFallback;
		}
	}

	[TestMethod]
	public void EnumFallback_ThrowsWhenReturnsNull()
	{
		var prevFallback = Converter.EnumFallback;
		try
		{
			Converter.EnumFallback = (enumType, value) => null;

			ThrowsExactly<InvalidCastException>(() =>
				"NonExistentValue".To<TestMigrationEnum>());
		}
		finally
		{
			Converter.EnumFallback = prevFallback;
		}
	}

	[TestMethod]
	public void EnumFallback_ThrowsWhenNotSet()
	{
		var prevFallback = Converter.EnumFallback;
		try
		{
			Converter.EnumFallback = null;

			ThrowsExactly<InvalidCastException>(() =>
				"NonExistentValue".To<TestMigrationEnum>());
		}
		finally
		{
			Converter.EnumFallback = prevFallback;
		}
	}
}
