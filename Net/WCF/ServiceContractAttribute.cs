namespace System.ServiceModel
{
	public class ServiceContractAttribute : Attribute
	{
		public string Namespace { get; set; }
	}

	/// <summary>Specifies whether a service class supports single-threaded or multi-threaded modes of operation. </summary>
	public enum ConcurrencyMode
	{
		/// <summary>The service instance is single-threaded and does not accept reentrant calls. If the <see cref="P:System.ServiceModel.ServiceBehaviorAttribute.InstanceContextMode" /> property is <see cref="F:System.ServiceModel.InstanceContextMode.Single" />, and additional messages arrive while the instance services a call, these messages must wait until the service is available or until the messages time out.</summary>
		Single,
		/// <summary>The service instance is single-threaded and accepts reentrant calls. The reentrant service accepts calls when you call another service; it is therefore your responsibility to leave your object state consistent before callouts and you must confirm that operation-local data is valid after callouts. Note that the service instance is unlocked only by calling another service over a WCF channel. In this case, the called service can reenter the first service via a callback. If the first service is not reentrant, the sequence of calls results in a deadlock. For details, see <see cref="P:System.ServiceModel.ServiceBehaviorAttribute.ConcurrencyMode" />. </summary>
		Reentrant,
		/// <summary>The service instance is multi-threaded. No synchronization guarantees are made. Because other threads can change your service object at any time, you must handle synchronization and state consistency at all times.</summary>
		Multiple,
	}

	public class CallbackBehaviorAttribute : Attribute
	{
		public ConcurrencyMode ConcurrencyMode { get; set; }
	}

	public class ExceptionDetail
	{
		public string Type { get; set; }
	}

	public class FaultException<T> : Exception
	{
		public T Detail { get; set; }
	}
}