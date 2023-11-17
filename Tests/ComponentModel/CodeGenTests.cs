namespace Ecng.Tests.ComponentModel;

using System;
using System.Text;

using Ecng.ComponentModel;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CodeGenTests
{
	[TestMethod]
	public void CodeGen()
	{
		var sb = new StringBuilder();

		ICodeGenVisitor visitor = new StringBuilderCodeGenVisitor(sb);

		visitor
			.AddLine("class Class1")
			.AddLine("{")
			.AddLine("public int Prop1 { get; set; }")
			.AddLine("}");

		sb.ToString().AssertEqual(@"class Class1
{
	public int Prop1 { get; set; }
}
");
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Braces()
	{
		var sb = new StringBuilder();

		ICodeGenVisitor visitor = new StringBuilderCodeGenVisitor(sb);

		visitor
			.AddLine("{")
			.AddLine("}")
			.AddLine("}");
	}
}
