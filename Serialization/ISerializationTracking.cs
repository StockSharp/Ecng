namespace Ecng.Serialization
{
	#region Using Directives

	#endregion

	public interface ISerializationTracking
	{
		void BeforeSerialize();
		void AfterSerialize();
		void BeforeDeserialize();
		void AfterDeserialize();
	}
}