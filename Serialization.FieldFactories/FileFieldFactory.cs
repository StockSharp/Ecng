namespace Ecng.Serialization
{
	using System.IO;

	using Ecng.Common;

	public class FileFieldFactory : FieldFactory<FileStream, string>
	{
		public FileFieldFactory(FileMode mode, FileAccess access, Field field, int order)
			: base(field, order)
		{
			Mode = mode;
			Access = access;
		}

		public FileMode Mode { get; private set; }
		public FileAccess Access { get; private set; }

		protected override FileStream OnCreateInstance(ISerializer serializer, string source)
		{
			return new FileStream(source, Mode, Access);

		}

		protected override string OnCreateSource(ISerializer serializer, FileStream instance)
		{
			return instance.Name;
		}

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			source.Add(new SerializationItem<FileMode>(new VoidField<FileMode>("Mode"), Mode));
			source.Add(new SerializationItem<FileAccess>(new VoidField<FileAccess>("Access"), Access));

			base.Serialize(serializer, fields, source);
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Mode = source["Order"].Value.To<FileMode>();
			Access = source["IsNullable"].Value.To<FileAccess>();

			base.Deserialize(serializer, fields, source);
		}
	}

	public class FileAttribute : FieldFactoryAttribute
	{
		public FileAttribute()
		{
			Mode = FileMode.OpenOrCreate;
			Access = FileAccess.ReadWrite;
		}

		public FileMode Mode { get; set; }
		public FileAccess Access { get; set; }

		public override FieldFactory CreateFactory(Field field)
		{
			return new FileFieldFactory(Mode, Access, field, Order);
		}
	}
}