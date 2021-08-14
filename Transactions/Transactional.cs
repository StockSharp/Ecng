namespace Ecng.Transactions
{
	#region Using Directives

	using System;
	using System.Transactions;

	using Ecng.Common;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public class Transactional<T> : Wrapper<T>, IEnlistmentNotification
	{
		#region Private Fields

		private readonly TransactionalLock _lock = new();
		private T _temporaryValue;
		private Transaction _currentTransaction;

		#endregion

		#region Transactional.ctor()

		public Transactional()
			: this(default)
		{
		}

		public Transactional(T value)
		{
			base.Value = value;
		}

		#endregion

		#region Wrapper<T> Members

		public override T Value
		{
			get
			{
				if (IsTransactionValid)
				{
					_lock.Lock();

					if (_currentTransaction is null)
						Enlist(base.Value);

					return _temporaryValue;
				}
				else
					return base.Value;
			}
			set
			{
				if (IsTransactionValid)
				{
					_lock.Lock();

					if (_currentTransaction is null)
						Enlist(value);
					else
						_temporaryValue = value;
				}
				else
					base.Value = value;
			}
		}

		#endregion

		#region IsChanged

		public bool IsChanged
		{
			get
			{
				if (IsTransactionValid)
					return !base.Value.Equals(_temporaryValue);
				else
					return false;
			}
		}

		#endregion

		#region IEnlistmentNotification Members

		void IEnlistmentNotification.Commit(Enlistment enlistment)
		{
			base.Value.DoDispose();
			base.Value = _temporaryValue;
			ReleaseData(enlistment);
		}

		void IEnlistmentNotification.InDoubt(Enlistment enlistment)
		{
			// Bad for a volatile resource, but not much that can be done about it
			_lock.Unlock();
			enlistment.Done();
		}

		void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
		{
			preparingEnlistment.Prepared();
		}

		void IEnlistmentNotification.Rollback(Enlistment enlistment)
		{
			_temporaryValue.DoDispose();
			ReleaseData(enlistment);
		}

		#endregion

		#region IsTransactionValid

		private static bool IsTransactionValid => Transaction.Current != null && Transaction.Current.TransactionInformation.Status == TransactionStatus.Active;

		#endregion

		#region Enlist

		private void Enlist(T value)
		{
			_currentTransaction = Transaction.Current;
			_currentTransaction.EnlistVolatile(this, EnlistmentOptions.None);
			_temporaryValue = CloneFactory<T>.Factory.Clone(value);
		}

		#endregion

		#region ReleaseData

		private void ReleaseData(Enlistment enlistment)
		{
			_currentTransaction = null;
			_temporaryValue = default;
			_lock.Unlock();
			enlistment.Done();
		}

		#endregion

		public override Wrapper<T> Clone()
		{
			throw new NotSupportedException();
		}
	}
}