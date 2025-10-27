namespace Ecng.Tests.Common;

[TestClass]
public class NullableHelperTests
{
	[TestMethod]
	public void GetUnderlyingType_NullableType_ReturnsUnderlyingType()
	{
		// Arrange
		var nullableType = typeof(int?);

		// Act
		var underlyingType = nullableType.GetUnderlyingType();

		// Assert
		underlyingType.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetUnderlyingType_NonNullableType_ReturnsNull()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var underlyingType = type.GetUnderlyingType();

		// Assert
		underlyingType.AssertNull();
	}

	[TestMethod]
	public void IsNullable_NullType_ThrowsArgumentNullException()
	{
		// Arrange
		Type type = null;

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => type.IsNullable());
	}

	[TestMethod]
	public void IsNullable_NullableType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(int?);

		// Act
		var result = type.IsNullable();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsNullable_NonNullableValueType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var result = type.IsNullable();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNullable_ReferenceType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.IsNullable();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNull_NullReferenceType_ReturnsTrue()
	{
		// Arrange
		string value = null;

		// Act
		var result = value.IsNull();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsNull_NonNullReferenceType_ReturnsFalse()
	{
		// Arrange
		string value = "test";

		// Act
		var result = value.IsNull();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNull_ValueType_ReturnsFalse()
	{
		// Arrange
		int value = 0;

		// Act
		var result = value.IsNull();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNull_WithCheckDefault_DefaultValue_ReturnsTrue()
	{
		// Arrange
		int value = 0;

		// Act
		var result = value.IsNull(checkValueTypeOnDefault: true);

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsNull_WithCheckDefault_NonDefaultValue_ReturnsFalse()
	{
		// Arrange
		int value = 42;

		// Act
		var result = value.IsNull(checkValueTypeOnDefault: true);

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void Convert_NullValue_CallsNullFunc()
	{
		// Arrange
		string value = null;
		var nullFuncCalled = false;
		var notNullFuncCalled = false;

		// Act
		var result = value.Convert(
			notNullFunc: v => { notNullFuncCalled = true; return v.Length; },
			nullFunc: () => { nullFuncCalled = true; return -1; }
		);

		// Assert
		result.AssertEqual(-1);
		nullFuncCalled.AssertTrue();
		notNullFuncCalled.AssertFalse();
	}

	[TestMethod]
	public void Convert_NonNullValue_CallsNotNullFunc()
	{
		// Arrange
		string value = "test";
		var nullFuncCalled = false;
		var notNullFuncCalled = false;

		// Act
		var result = value.Convert(
			notNullFunc: v => { notNullFuncCalled = true; return v.Length; },
			nullFunc: () => { nullFuncCalled = true; return -1; }
		);

		// Assert
		result.AssertEqual(4);
		notNullFuncCalled.AssertTrue();
		nullFuncCalled.AssertFalse();
	}

	[TestMethod]
	public void Convert_NullNotNullFunc_ThrowsArgumentNullException()
	{
		// Arrange
		string value = "test";

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			value.Convert<string, int>(null, () => 0));
	}

	[TestMethod]
	public void Convert_NullNullFunc_ThrowsArgumentNullException()
	{
		// Arrange
		string value = "test";

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			value.Convert<string, int>(v => v.Length, null));
	}

	[TestMethod]
	public void DefaultAsNull_DefaultValue_ReturnsNull()
	{
		// Arrange
		int value = 0;

		// Act
		var result = value.DefaultAsNull();

		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void DefaultAsNull_NonDefaultValue_ReturnsValue()
	{
		// Arrange
		int value = 42;

		// Act
		var result = value.DefaultAsNull();

		// Assert
		result.HasValue.AssertTrue();
		result.Value.AssertEqual(42);
	}

	[TestMethod]
	public void MakeNullable_ValueType_ReturnsNullableType()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var nullableType = type.MakeNullable();

		// Assert
		nullableType.AssertEqual(typeof(int?));
	}

	[TestMethod]
	public void TryMakeNullable_NonNullableValueType_ReturnsNullableType()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var result = type.TryMakeNullable();

		// Assert
		result.AssertEqual(typeof(int?));
	}

	[TestMethod]
	public void TryMakeNullable_AlreadyNullableType_ReturnsSameType()
	{
		// Arrange
		var type = typeof(int?);

		// Act
		var result = type.TryMakeNullable();

		// Assert
		result.AssertEqual(typeof(int?));
	}

	[TestMethod]
	public void TryMakeNullable_ReferenceType_ReturnsSameType()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.TryMakeNullable();

		// Assert
		result.AssertEqual(typeof(string));
	}

	[TestMethod]
	public void IsNull_NullableValueTypeWithValue_ReturnsFalse()
	{
		// Arrange
		int? value = 42;

		// Act
		var result = value.IsNull();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNull_NullableValueTypeNull_ReturnsTrue()
	{
		// Arrange
		int? value = null;

		// Act
		var result = value.IsNull();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsNull()
	{
		1.IsNull().AssertFalse();
		0.IsNull().AssertFalse();
		0.IsNull(true).AssertTrue();
		default(int).IsNull().AssertFalse();
		default(int?).IsNull().AssertTrue();
		default(int).IsNull(true).AssertTrue();
		default(int?).IsNull(true).AssertTrue();

		TimeSpan.Zero.IsNull().AssertFalse();
		TimeSpan.Zero.IsNull(true).AssertTrue();
		default(TimeSpan).IsNull().AssertFalse();
		default(TimeSpan?).IsNull().AssertTrue();
		default(TimeSpan).IsNull(true).AssertTrue();
		default(TimeSpan?).IsNull(true).AssertTrue();

		string.Empty.IsNull().AssertFalse();
		string.Empty.IsNull(true).AssertFalse();
		default(string).IsNull().AssertTrue();
		default(string).IsNull(true).AssertTrue();

		object o1 = 1;
		object o2 = 0;
		object o3 = default(int);
		object o4 = default(int?);

		o1.IsNull().AssertFalse();
		o1.IsNull(true).AssertFalse();
		o2.IsNull().AssertFalse();
		o2.IsNull(true).AssertTrue();
		o3.IsNull().AssertFalse();
		o3.IsNull(true).AssertTrue();
		o4.IsNull().AssertTrue();
		o4.IsNull(true).AssertTrue();

		o1 = TimeSpan.FromSeconds(1);
		o2 = TimeSpan.Zero;
		o3 = default(TimeSpan);
		o4 = default(TimeSpan?);

		o1.IsNull().AssertFalse();
		o1.IsNull(true).AssertFalse();
		o2.IsNull().AssertFalse();
		o2.IsNull(true).AssertTrue();
		o3.IsNull().AssertFalse();
		o3.IsNull(true).AssertTrue();
		o4.IsNull().AssertTrue();
		o4.IsNull(true).AssertTrue();

		o1 = "1";
		o2 = "";
		o3 = default(string);
		o4 = string.Empty;

		o1.IsNull().AssertFalse();
		o1.IsNull(true).AssertFalse();
		o2.IsNull().AssertFalse();
		o2.IsNull(true).AssertFalse();
		o3.IsNull().AssertTrue();
		o3.IsNull(true).AssertTrue();
		o4.IsNull().AssertFalse();
		o4.IsNull(true).AssertFalse();
	}
}
