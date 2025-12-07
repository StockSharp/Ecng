namespace Ecng.Tests.ComponentModel;

using System.Text;

using Ecng.ComponentModel;

[TestClass]
public class CodeGenTests : BaseTestClass
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

		sb.ToString().Replace(StringHelper.RN, StringHelper.N).AssertEqual(@"class Class1
{
	public int Prop1 { get; set; }
}
".Replace(StringHelper.RN, StringHelper.N));
	}

	[TestMethod]
	public void Braces()
	{
		var sb = new StringBuilder();

		ICodeGenVisitor visitor = new StringBuilderCodeGenVisitor(sb);

		visitor
			.AddLine("{")
			.AddLine("}");

		ThrowsExactly<InvalidOperationException>(() => visitor.AddLine("}"));
	}

	[TestMethod]
	public void Braces2()
	{
		var sb = new StringBuilder();

		ICodeGenVisitor visitor = new StringBuilderCodeGenVisitor(sb);

		ThrowsExactly<InvalidOperationException>(() => visitor.AddLine("}"));
	}

	[TestMethod]
	public void Indent()
	{
		var sb = new StringBuilder();

		ICodeGenVisitor visitor = new StringBuilderCodeGenVisitor(sb);

		visitor.CurrIndent.AssertEqual(0);

		visitor.AddLine("{");
		visitor.CurrIndent.AssertEqual(1);

		visitor.AddLine("}");
		visitor.CurrIndent.AssertEqual(0);

		visitor.AddLine("{");
		visitor.CurrIndent.AssertEqual(1);

		visitor.AddLine("{");
		visitor.CurrIndent.AssertEqual(2);
	}
}
