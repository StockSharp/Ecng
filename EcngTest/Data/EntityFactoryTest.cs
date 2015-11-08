namespace Ecng.Test.Data
{
	#region Using Directives

	using System;

	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	class EmptyEntityFactory<T> : EntityFactory<T>
	{
		public override bool FullInitialize => false;

		public override T CreateEntity(ISerializer serializer, SerializationItemCollection itemSource)
		{
			return Activator.CreateInstance<T>();
		}
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityFactory(typeof(EmptyEntityFactory<EntityFactoryStructEntity>))]
	public struct EntityFactoryStructEntity
	{
		#region Id

		private long _id;

		[Identity]
		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityFactory(typeof(EmptyEntityFactory<EntityFactoryClassEntity>))]
	public class EntityFactoryClassEntity
	{
		#region Id

		private long _id;

		[Identity]
		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion
	}

	[TestClass]
	public class EntityFactoryTest
	{
		[TestMethod]
		public void Struct()
		{
			typeof(EntityFactoryStructEntity).GetSchema().GetFactory<EntityFactoryStructEntity>().CreateEntity(null, null);
		}

		[TestMethod]
		public void Class()
		{
			typeof(EntityFactoryStructEntity).GetSchema().GetFactory<EntityFactoryStructEntity>().CreateEntity(null, null);
		}
	}
}