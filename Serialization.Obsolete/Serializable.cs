namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	public abstract class Serializable<T> : Equatable<T>, ISerializable//, IXmlSerializable, ISerializable, ISerializationTracking
		where T : class
	{
		#region ISerializable Members

		Task ISerializable.Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Serialize(serializer, fields, source, cancellationToken);

		Task ISerializable.Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Deserialize(serializer, fields, source, cancellationToken);

		#endregion

		/// <summary>
		/// Serializes object into specified source.
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="fields"></param>
		/// <param name="source">Serialized state.</param>
		protected abstract Task Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);

		/// <summary>
		/// Deserialize object into specified source.
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="fields"></param>
		/// <param name="source">Serialized state.</param>
		protected abstract Task Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);

		#region Cloneable<T> Members

		public override T Clone()
		{
			return CloneFactory<T>.Factory.Clone(this.To<T>());
		}

		#endregion
	}
}