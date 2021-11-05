namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	public abstract class Serializable<T> : Equatable<T>, ISerializable//, IXmlSerializable, ISerializable, ISerializationTracking
		where T : class
	{
		#region ISerializable Members

		void ISerializable.Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Serialize(serializer, fields, source);
		}

		void ISerializable.Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Deserialize(serializer, fields, source);
		}

		#endregion

		/// <summary>
		/// Serializes object into specified source.
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="fields"></param>
		/// <param name="source">Serialized state.</param>
		protected abstract void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source);

		/// <summary>
		/// Deserialize object into specified source.
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="fields"></param>
		/// <param name="source">Serialized state.</param>
		protected abstract void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source);

		#region Cloneable<T> Members

		public override T Clone()
		{
			return CloneFactory<T>.Factory.Clone(this.To<T>());
		}

		#endregion
	}
}