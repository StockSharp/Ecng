namespace Ecng.Security
{
	#region Using Directives

	using System.Linq;
	using System.Security.Cryptography.X509Certificates;

	using Ecng.Common;

	#endregion

	public class X509StoreEx : Disposable
	{
		#region private class X509Certificate2CollectionEx

		private sealed class X509Certificate2CollectionEx : X509Certificate2Collection
		{
			#region Private Fields

			private readonly X509Store _store;

			#endregion

			#region X509Certificate2CollectionEx.ctor()

			public X509Certificate2CollectionEx(X509Store store)
				: base(store.Certificates)
			{
				_store = store;
			}

			#endregion

			#region CollectionBase Members

			protected override void OnInsertComplete(int index, object value)
			{
				if (_store != null)
					_store.Add((X509Certificate2)value);

				base.OnInsertComplete(index, value);
			}

			protected override void OnSetComplete(int index, object oldValue, object newValue)
			{
				if (_store != null)
				{
					if (oldValue != null)
						_store.Remove((X509Certificate2)oldValue);

					_store.Add((X509Certificate2)newValue);
				}

				base.OnSetComplete(index, oldValue, newValue);
			}

			protected override void OnRemoveComplete(int index, object value)
			{
				if (_store != null)
					_store.Remove((X509Certificate2)value);

				base.OnRemoveComplete(index, value);
			}

			protected override void OnClearComplete()
			{
				if (_store != null)
					_store.RemoveRange(new X509Certificate2Collection(InnerList.Cast<X509Certificate2>().ToArray()));

				base.OnClearComplete();
			}

			#endregion
		}

		#endregion

		#region Private Fields

		private readonly X509Store _store;

		#endregion

		#region X509StoreEx.ctor()

		public X509StoreEx(OpenFlags flags)
		{
			_store = new X509Store();
			Open(flags);
		}

		public X509StoreEx(StoreLocation location, OpenFlags flags)
		{
			_store = new X509Store(location);
			Open(flags);
		}

		public X509StoreEx(StoreName name, OpenFlags flags)
		{
			_store = new X509Store(name);
			Open(flags);
		}

		public X509StoreEx(string name, OpenFlags flags)
		{
			_store = new X509Store(name);
			Open(flags);
		}

		public X509StoreEx(StoreName name, StoreLocation location, OpenFlags flags)
		{
			_store = new X509Store(name, location);
			Open(flags);
		}

		public X509StoreEx(string name, StoreLocation location, OpenFlags flags)
		{
			_store = new X509Store(name, location);
			Open(flags);
		}

		#endregion

		#region Name

		public string Name => _store.Name;

		#endregion

		#region Location

		public StoreLocation Location => _store.Location;

		#endregion

		#region Certificates

		private X509Certificate2CollectionEx _certificates;

		public X509Certificate2Collection Certificates => _certificates;

		#endregion

		#region Open

		private void Open(OpenFlags flags)
		{
			_store.Open(flags);
			_certificates = new X509Certificate2CollectionEx(_store);
		}

		#endregion
		
		#region Disposable Members

		protected override void DisposeManaged()
		{
			_store.Close();
			base.DisposeManaged();
		}

		#endregion
	}
}