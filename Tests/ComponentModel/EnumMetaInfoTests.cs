namespace Ecng.Tests.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EnumMetaInfoTests
{
	[Flags]
	private enum TestFlags
	{
		F1 = 1,
		F2 = F1 << 1
	}

	[TestMethod]
	public void Enums()
	{
		TestFlags.F1.GetDisplayName().AssertEqual("F1");
		TestFlags.F1.GetFieldDisplayName().AssertEqual("F1");
		TestFlags.F1.GetFieldDescription().AssertEqual(string.Empty);
		TestFlags.F1.GetFieldIcon().AssertNull();

		(TestFlags.F1 | TestFlags.F2).GetDisplayName().AssertEqual("F1, F2");
		(TestFlags.F1 | TestFlags.F2).GetFieldDisplayName().AssertEqual("F1, F2");
		(TestFlags.F1 | TestFlags.F2).GetFieldDescription().AssertEqual(string.Empty);
		(TestFlags.F1 | TestFlags.F2).GetFieldIcon().AssertNull();
	}

	private enum TestFlags2
	{
		F1 = 1,
		F2 = F1 << 1
	}

	[TestMethod]
	public void EnumsNoFlags()
	{
		TestFlags2.F1.GetDisplayName().AssertEqual("F1");
		TestFlags2.F1.GetFieldDisplayName().AssertEqual("F1");
		TestFlags2.F1.GetFieldDescription().AssertEqual(string.Empty);
		TestFlags2.F1.GetFieldIcon().AssertNull();

		(TestFlags2.F1 | TestFlags2.F2).GetDisplayName().AssertEqual("3");
		(TestFlags2.F1 | TestFlags2.F2).GetFieldDisplayName().AssertEqual("3");
		(TestFlags2.F1 | TestFlags2.F2).GetFieldDescription().AssertEqual(string.Empty);
		(TestFlags2.F1 | TestFlags2.F2).GetFieldIcon().AssertNull();
	}

	[Flags]
	private enum NameTestFlags
	{
		[Display(Name = "F 1")]
		F1 = 1,

		[Display(Name = "F 2")]
		F2 = F1 << 1
	}

	[TestMethod]
	public void NameEnums()
	{
		NameTestFlags.F1.GetDisplayName().AssertEqual("F 1");
		NameTestFlags.F1.GetFieldDisplayName().AssertEqual("F 1");
		NameTestFlags.F1.GetFieldDescription().AssertEqual(string.Empty);
		NameTestFlags.F1.GetFieldIcon().AssertNull();

		(NameTestFlags.F1 | NameTestFlags.F2).GetDisplayName().AssertEqual("F 1, F 2");
		(NameTestFlags.F1 | NameTestFlags.F2).GetFieldDisplayName().AssertEqual("F 1, F 2");
		(NameTestFlags.F1 | NameTestFlags.F2).GetFieldDescription().AssertEqual(string.Empty);
		(NameTestFlags.F1 | NameTestFlags.F2).GetFieldIcon().AssertNull();
	}

	private enum NameTestFlags2
	{
		[Display(Name = "F 1")]
		F1 = 1,

		[Display(Name = "F 2")]
		F2 = F1 << 1
	}

	[TestMethod]
	public void NameEnumsNoFlags()
	{
		NameTestFlags2.F1.GetDisplayName().AssertEqual("F 1");
		NameTestFlags2.F1.GetFieldDisplayName().AssertEqual("F 1");
		NameTestFlags2.F1.GetFieldDescription().AssertEqual(string.Empty);
		NameTestFlags2.F1.GetFieldIcon().AssertNull();

		(NameTestFlags2.F1 | NameTestFlags2.F2).GetDisplayName().AssertEqual("3");
		(NameTestFlags2.F1 | NameTestFlags2.F2).GetFieldDisplayName().AssertEqual("3");
		(NameTestFlags2.F1 | NameTestFlags2.F2).GetFieldDescription().AssertEqual(string.Empty);
		(NameTestFlags2.F1 | NameTestFlags2.F2).GetFieldIcon().AssertNull();
	}

	private enum TestEnum
	{
		F1,
		F2,
	}

	[TestMethod]
	public void NoFlagsEnums()
	{
		TestEnum.F1.GetDisplayName().AssertEqual("F1");
		TestEnum.F1.GetFieldDisplayName().AssertEqual("F1");
		TestEnum.F1.GetFieldDescription().AssertEqual(string.Empty);
		TestEnum.F1.GetFieldIcon().AssertNull();
	}

	private enum LocTestEnum
	{
		[Display(Name = "F 1")]
		F1,
		[Display(Name = "F 2")]
		F2,
	}

	[TestMethod]
	public void LocNoFlagsEnums()
	{
		LocTestEnum.F1.GetDisplayName().AssertEqual("F 1");
		LocTestEnum.F1.GetFieldDisplayName().AssertEqual("F 1");
		LocTestEnum.F1.GetFieldDescription().AssertEqual(string.Empty);
		LocTestEnum.F1.GetFieldIcon().AssertNull();
	}

	[Flags]
	private enum ZeroFieldFlags
	{
		F0 = 0,
		F1 = 1,
		F2 = F1 << 1
	}

	[Flags]
	private enum LocZeroFieldFlags
	{
		[Display(Name = "F 0")]
		F0 = 0,

		[Display(Name = "F 1")]
		F1 = 1,

		[Display(Name = "F 2")]
		F2 = F1 << 1
	}

	[TestMethod]
	public void FieldsZeroFieldFlags()
	{
		ZeroFieldFlags.F1.GetDisplayName().AssertEqual("F1");
		ZeroFieldFlags.F1.GetFieldDisplayName().AssertEqual("F1");
		ZeroFieldFlags.F1.GetFieldDescription().AssertEqual(string.Empty);
		ZeroFieldFlags.F1.GetFieldIcon().AssertNull();

		(ZeroFieldFlags.F1 | ZeroFieldFlags.F2).GetDisplayName().AssertEqual("F1, F2");
		(ZeroFieldFlags.F1 | ZeroFieldFlags.F2).GetFieldDisplayName().AssertEqual("F1, F2");
		(ZeroFieldFlags.F1 | ZeroFieldFlags.F2).GetFieldDescription().AssertEqual(string.Empty);
		(ZeroFieldFlags.F1 | ZeroFieldFlags.F2).GetFieldIcon().AssertNull();

		LocZeroFieldFlags.F1.GetDisplayName().AssertEqual("F 1");
		LocZeroFieldFlags.F1.GetFieldDisplayName().AssertEqual("F 1");
		LocZeroFieldFlags.F1.GetFieldDescription().AssertEqual(string.Empty);
		LocZeroFieldFlags.F1.GetFieldIcon().AssertNull();

		(LocZeroFieldFlags.F1 | LocZeroFieldFlags.F2).GetDisplayName().AssertEqual("F 1, F 2");
		(LocZeroFieldFlags.F1 | LocZeroFieldFlags.F2).GetFieldDisplayName().AssertEqual("F 1, F 2");
		(LocZeroFieldFlags.F1 | LocZeroFieldFlags.F2).GetFieldDescription().AssertEqual(string.Empty);
		(LocZeroFieldFlags.F1 | LocZeroFieldFlags.F2).GetFieldIcon().AssertNull();
	}
}