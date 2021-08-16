namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<Field>))]
	public class Field : Equatable<Field>
	{
		private int _hashCode;

		#region Field.ctor()

		public Field(Schema schema, MemberInfo member)
		{
			Schema = schema ?? throw new ArgumentNullException(nameof(schema));
			Member = member ?? throw new ArgumentNullException(nameof(member));
			Init(member.Name, member.GetMemberType());
		}

		protected Field(string name, Type type)
		{
			Init(name, type);
		}

		private void Init(string name, Type type)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));
			Name = name;
			Type = type ?? throw new ArgumentNullException(nameof(type));
			InnerSchemaNameOverrides = new PairSet<string, string>();
			InnerSchemaIgnoreFields = new List<string>();
			OrderedIndex = int.MaxValue;

			_hashCode = (Schema != null ? Schema.GetHashCode() : 397) ^ Name.GetHashCode();
		}

		#endregion

		[Ignore]
		public Schema Schema { get; }

		[Identity]
		[Member]
		public MemberInfo Member { get; }

		public string Name { get; internal set; }

		[Ignore]
		public Type Type { get; set; }

		public bool IsReadOnly { get; set; }

		public bool IsIndex { get; set; }

		public bool IsUnderlying { get; set; }

		public PairSet<string, string> InnerSchemaNameOverrides { get; private set; }

		public IList<string> InnerSchemaIgnoreFields { get; private set; }

		[Underlying]
		public FieldFactory Factory { get; set; }

		public int OrderedIndex { get; set; }

		[Underlying]
		public FieldAccessor Accessor { get; set; }

		#region Equatable<Field> Members

		protected override bool OnEquals(Field other)
		{
			return Schema == other.Schema && Name == other.Name;
		}

		#endregion

		#region Cloneable<Field> Members

		public override Field Clone()
		{
			return CloneFactory<Field>.Factory.Clone(this);
		}

		#endregion

		#region Object Members

		public override string ToString()
		{
			return $"Name: {Name}, Type: {Type.Name}";
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		#endregion
	}
}