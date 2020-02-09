namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Web;

	sealed class AuditPageLoadAttribute : RelationManyAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			UnderlyingListType = typeof(BaseEntityAuditList);//.Make(field.Type.GetGenericArguments());
			return base.GetFactoryType(field);
		}
	}

	[Serializable]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	public abstract class BaseEntity : Equatable<BaseEntity>
	{
		protected BaseEntity()
		{
			Id = Extensions.DefaultId;
			CreationDate = ModificationDate = DateTime.Now;
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		public long Id { get; set; }

		[AuditField((byte)AuditFields.CreationDate)]
		public DateTime CreationDate { get; set; }

		//[AuditField((byte)AuditFields.ModificationDate)]
		public DateTime ModificationDate { get; set; }

		[AuditField((byte)AuditFields.Deleted)]
		public bool Deleted { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.User)]
		public IWebUser CreatedBy { get; set; }

		[NonSerialized]
		private AuditList _changes;

		[AuditPageLoad]
		[IgnoreDataMember]
		public AuditList Changes
		{
			get { return _changes; }
			protected set { _changes = value; }
		}

		#region Equatable<BaseEntity> Members

		protected override bool OnEquals(BaseEntity other)
		{
			if (!this.IsNotSaved())
				return Id == other.Id;
			else
				return ReferenceEquals(this, other);
		}

		#endregion

		#region Cloneable<BaseEntity> Members

		public override BaseEntity Clone()
		{
			throw new NotSupportedException();
		}

		#endregion

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override string ToString()
		{
			return "{{Type={0}, Id={1}}}".Put(GetType(), Id);
		}
	}
}