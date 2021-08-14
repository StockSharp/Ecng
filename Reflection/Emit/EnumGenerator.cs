namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System.Collections.Generic;
	using System.Reflection.Emit;

	#endregion

	public class EnumFieldGenerator : FieldGenerator
	{
		#region EnumFieldGenerator.ctor()

		internal EnumFieldGenerator(FieldBuilder builder)
			: base(builder)
		{
		}

		#endregion
	}

	public class EnumGenerator : BaseGenerator<EnumBuilder>
	{
		#region EnumGenerator.ctor()

		internal EnumGenerator(EnumBuilder builder)
			: base(builder)
		{
		}

		#endregion

		#region Fields

		private readonly List<EnumFieldGenerator> _fields = new();

		public IEnumerable<EnumFieldGenerator> Fields => _fields;

		#endregion

		public EnumFieldGenerator CreateField(string name, object value)
		{
			var generator = new EnumFieldGenerator(Builder.DefineLiteral(name, value));
			_fields.Add(generator);
			return generator;
		}
	}
}