namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	#endregion

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	public class Schema : Equatable<Schema>
	{
		public Schema()
		{
			Fields = new FieldList();
		}

		#region EntityType

		private Type _entityType;

		[Identity]
		[Member]
		public Type EntityType
		{
			get { return _entityType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_entityType = value;
				Name = value.Name;
			}
		}

		#endregion

		#region Name

		public string Name { get; set; }

		#endregion

		#region Identity

		private IdentityField _identity;

		[Ignore]
		public IdentityField Identity
		{
			get { return _identity ?? (_identity = (IdentityField)Fields.FirstOrDefault(arg => arg is IdentityField)); }
		}

		#endregion

		#region ReadOnly

		[Ignore]
		public bool ReadOnly
		{
			get { return Fields.NonReadOnlyFields.IsEmpty(); }
		}

		#endregion

		#region IsSerializable

		[Ignore]
		public bool IsSerializable
		{
			get { return typeof(ISerializable).IsAssignableFrom(EntityType); }
		}

		#endregion

		#region Fields

		[Collection]
		public FieldList Fields { get; private set; }

		#endregion

		#region Factory

		[Underlying]
		public EntityFactory Factory { get; set; }

		#endregion

		#region Equatable<Schema> Members

		protected override bool OnEquals(Schema other)
		{
			return EntityType == other.EntityType;
		}

		#endregion

		#region Cloneable<Schema> Members

		public override Schema Clone()
		{
			return CloneFactory<Schema>.Factory.Clone(this);
		}

		#endregion

		#region Object Members

		public override string ToString()
		{
			return "Name: {0}, Type: {1}".Put(Name, EntityType.Name);
		}

		public override int GetHashCode()
		{
			return EntityType.GetHashCode();
		}

		#endregion
	}
}