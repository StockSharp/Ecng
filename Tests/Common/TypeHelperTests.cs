namespace Ecng.Tests.Common;

using System.Dynamic;

[TestClass]
public class TypeHelperTests : BaseTestClass
{
	private enum TestEnum
	{
		Value1,
		Value2,
		Value3
	}

	private struct TestStruct
	{
		public int Value { get; set; }
	}

	[TestMethod]
	public void CreateInstance_NullType_ThrowsArgumentNullException()
	{
		// Arrange
		Type type = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => type.CreateInstance());
	}

	[TestMethod]
	public void CreateInstance_WithNoArgs_CreatesInstance()
	{
		// Arrange
		var type = typeof(List<int>);

		// Act
		var instance = type.CreateInstance();

		// Assert
		instance.AssertNotNull();
		(instance is List<int>).AssertTrue();
	}

	[TestMethod]
	public void CreateInstance_WithArgs_CreatesInstanceWithArgs()
	{
		// Arrange
		var type = typeof(List<int>);

		// Act
		var instance = type.CreateInstance<List<int>>(10);

		// Assert
		instance.AssertNotNull();
		instance.Capacity.AssertEqual(10);
	}

	[TestMethod]
	public void Make_NullType_ThrowsArgumentNullException()
	{
		// Arrange
		Type type = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => type.Make(typeof(int)));
	}

	[TestMethod]
	public void Make_WithTypeArgs_CreatesGenericType()
	{
		// Arrange
		var genericType = typeof(List<>);

		// Act
		var constructedType = genericType.Make(typeof(string));

		// Assert
		constructedType.AssertEqual(typeof(List<string>));
	}

	[TestMethod]
	public void IsPrimitive_IntType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var result = type.IsPrimitive();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsPrimitive_StringType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.IsPrimitive();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsPrimitive_DateTimeType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(DateTime);

		// Act
		var result = type.IsPrimitive();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsPrimitive_EnumType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(TestEnum);

		// Act
		var result = type.IsPrimitive();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsPrimitive_ComplexType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(List<int>);

		// Act
		var result = type.IsPrimitive();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsNumeric_IntType_ReturnsTrue()
	{
		// Assert
		typeof(int).IsNumeric().AssertTrue();
		typeof(long).IsNumeric().AssertTrue();
		typeof(double).IsNumeric().AssertTrue();
		typeof(decimal).IsNumeric().AssertTrue();
		typeof(byte).IsNumeric().AssertTrue();
	}

	[TestMethod]
	public void IsNumeric_NonNumericType_ReturnsFalse()
	{
		// Assert
		typeof(string).IsNumeric().AssertFalse();
		typeof(DateTime).IsNumeric().AssertFalse();
		typeof(bool).IsNumeric().AssertFalse();
	}

	[TestMethod]
	public void IsNumericInteger_IntegerTypes_ReturnsTrue()
	{
		// Assert
		typeof(int).IsNumericInteger().AssertTrue();
		typeof(long).IsNumericInteger().AssertTrue();
		typeof(byte).IsNumericInteger().AssertTrue();
		typeof(short).IsNumericInteger().AssertTrue();
	}

	[TestMethod]
	public void IsNumericInteger_FloatingPointTypes_ReturnsFalse()
	{
		// Assert
		typeof(double).IsNumericInteger().AssertFalse();
		typeof(decimal).IsNumericInteger().AssertFalse();
		typeof(float).IsNumericInteger().AssertFalse();
	}

	[TestMethod]
	public void IsStruct_StructType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(TestStruct);

		// Act
		var result = type.IsStruct();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsStruct_EnumType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(TestEnum);

		// Act
		var result = type.IsStruct();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsStruct_ClassType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.IsStruct();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsEnum_EnumType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(TestEnum);

		// Act
		var result = type.IsEnum();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsEnum_NonEnumType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var result = type.IsEnum();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsAttribute_AttributeType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(ObsoleteAttribute);

		// Act
		var result = type.IsAttribute();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsDelegate_DelegateType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(Action);

		// Act
		var result = type.IsDelegate();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void GetDefaultValue_ReferenceType_ReturnsNull()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.GetDefaultValue();

		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void GetDefaultValue_ValueType_ReturnsDefault()
	{
		// Arrange
		var type = typeof(int);

		// Act
		var result = type.GetDefaultValue();

		// Assert
		result.AssertEqual(0);
	}

	[TestMethod]
	public void SingleOrAggr_SingleException_ReturnsSingleException()
	{
		// Arrange
		var ex = new InvalidOperationException("test");
		var errors = new List<Exception> { ex };

		// Act
		var result = errors.SingleOrAggr();

		// Assert
		result.AssertEqual(ex);
	}

	[TestMethod]
	public void SingleOrAggr_MultipleExceptions_ReturnsAggregateException()
	{
		// Arrange
		var errors = new List<Exception>
		{
			new InvalidOperationException("test1"),
			new ArgumentException("test2")
		};

		// Act
		var result = errors.SingleOrAggr();

		// Assert
		(result is AggregateException).AssertTrue();
		var aggEx = (AggregateException)result;
		aggEx.InnerExceptions.Count.AssertEqual(2);
	}

	[TestMethod]
	public void CheckOnNull_NonNullValue_ReturnsValue()
	{
		// Arrange
		var value = "test";

		// Act
		var result = value.CheckOnNull();

		// Assert
		result.AssertEqual("test");
	}

	[TestMethod]
	public void CheckOnNull_NullValue_ThrowsArgumentNullException()
	{
		// Arrange
		string value = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => value.CheckOnNull());
	}

	[TestMethod]
	public void HiWord_Value_ExtractsHighWord()
	{
		// Arrange
		var value = 0x12345678;

		// Act
		var result = value.HiWord();

		// Assert
		result.AssertEqual(0x1234);
	}

	[TestMethod]
	public void LoWord_Value_ExtractsLowWord()
	{
		// Arrange
		var value = 0x12345678;

		// Act
		var result = value.LoWord();

		// Assert
		result.AssertEqual(0x5678);
	}

	[TestMethod]
	public void HasProperty_ExistingProperty_ReturnsTrue()
	{
		// Arrange
		var obj = new { Name = "test", Value = 123 };

		// Act
		var result = obj.HasProperty("Name");

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void HasProperty_MissingProperty_ReturnsFalse()
	{
		// Arrange
		var obj = new { Name = "test" };

		// Act
		var result = obj.HasProperty("Missing");

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void HasProperty_ExpandoObject_ChecksCorrectly()
	{
		// Arrange
		dynamic expando = new ExpandoObject();
		expando.Name = "test";

		// Act
		var result = ((object)expando).HasProperty("Name");

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void Is_SameType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(List<int>);

		// Act
		var result = type.Is<List<int>>();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void Is_DerivedType_ReturnsTrue()
	{
		// Arrange
		var type = typeof(ArgumentNullException);

		// Act
		var result = type.Is<Exception>();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void Is_UnrelatedType_ReturnsFalse()
	{
		// Arrange
		var type = typeof(string);

		// Act
		var result = type.Is<int>();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsValidWebLink_ValidHttpUrl_ReturnsTrue()
	{
		// Arrange
		var link = "https://www.example.com";

		// Act
		var result = link.IsValidWebLink();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsValidWebLink_InvalidUrl_ReturnsFalse()
	{
		// Arrange
		var link = "not a url";

		// Act
		var result = link.IsValidWebLink();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsWebLink_HttpUri_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("http://example.com");

		// Act
		var result = uri.IsWebLink();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsWebLink_HttpsUri_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("https://example.com");

		// Act
		var result = uri.IsWebLink();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsWebLink_FtpUri_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("ftp://example.com");

		// Act
		var result = uri.IsWebLink();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsWebLink_FileUri_ReturnsFalse()
	{
		// Arrange
		var uri = new Uri("file:///c:/test.txt");

		// Act
		var result = uri.IsWebLink();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void HasProperty()
	{
		var obj = new { MyProp = 123 };
		var propName = nameof(obj.MyProp);

		obj.HasProperty(propName).AssertTrue();

		var obj2 = new { };
		obj2.HasProperty(propName).AssertFalse();

		dynamic obj3 = new ExpandoObject();
		obj3.MyProp = 123;
		((object)obj3).HasProperty(propName).AssertTrue();

		dynamic obj4 = new ExpandoObject();
		TypeHelper.HasProperty((object)obj4, propName).AssertFalse();
	}
}
