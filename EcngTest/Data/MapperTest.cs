namespace Ecng.Test.Data
{
	#region Using Directives

	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[EntityExtension]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class MappingEntity
	{
		#region Id

		[Identity]
		[FieldAccessor(typeof(TestAccessor))]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion
	}

	class TestAccessor : FieldAccessor<MappingEntity>
	{
		#region TestAccessor.ctor()

		public TestAccessor(Field field)
			: base(field)
		{

		}

		#endregion

		public override object GetValue(MappingEntity entity)
		{
			return entity.Id;
		}

		public override MappingEntity SetValue(MappingEntity entity, object value)
		{
			entity.Id = (long)value;
			return entity;
		}
	}

	[TestClass]
	public class MapperTest
	{
		[TestMethod]
		public void Simple()
		{
			var entity = Config.Create<MappingEntity>();

			var serializer = new BinarySerializer<MappingEntity>();

			var source = new SerializationItemCollection { new SerializationItem(Serializer<MappingEntity>.Schema.Identity, 10) };
			var entity2 = serializer.Deserialize(source);
			Assert.AreEqual(10L, entity2.Id);

			source = new SerializationItemCollection();
			serializer.Serialize(entity, source);

			Assert.AreEqual(10L, (long)source["Id"].Value);
		}
	}
}