namespace Ecng.Transactions
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Transactions;

	using Ecng.Collections;
	using Ecng.Common;

	class TransactionalLock : SyncObject
	{
		#region Private Fields

		private readonly LinkedList<KeyValuePair<Transaction, ManualResetEvent>> _pendingTransactions = new LinkedList<KeyValuePair<Transaction, ManualResetEvent>>();

		#endregion

		#region OwningTransaction

		public Transaction OwningTransaction { get; set; }

		#endregion

		#region Locked

		public bool Locked => (OwningTransaction != null);

		#endregion

		#region Lock

		public void Lock()
		{
			Lock(Transaction.Current);
		}

		public void Lock(Transaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			Enter();

			if (OwningTransaction == null)
			{
				//Acquire the transaction lock
				OwningTransaction = transaction;
				Exit();
			}
			else //Some transaction owns the lock
			{
				//We're done if it's the same one as the method parameter
				if (OwningTransaction == transaction)
					Exit();
				else //Otherwise, need to acquire the transaction lock
				{
					var manualEvent = new ManualResetEvent(false);

					var pair = new KeyValuePair<Transaction, ManualResetEvent>(transaction, manualEvent);
					_pendingTransactions.AddLast(pair);

					transaction.TransactionCompleted += delegate
					{
						lock (this)
						{
							//Pair may have already been removed if unlocked
							_pendingTransactions.Remove(pair);
						}

						lock (manualEvent)
						{
							if (!manualEvent.SafeWaitHandle.IsClosed)
								manualEvent.Set();
						}
					};

					Exit();

					//Block the transaction or the calling thread
					manualEvent.WaitOne();

					lock (manualEvent)
						manualEvent.Close();
				}
			}
		}

		#endregion

		#region Unlock

		public void Unlock()
		{
			//Debug.Assert(Locked);

			lock (this)
			{
				OwningTransaction = null;

				if (!_pendingTransactions.IsEmpty())
				{
					var pair = _pendingTransactions.First.Value;
					_pendingTransactions.RemoveFirst();

					//Transaction transaction = pair.Key;
					//ManualResetEvent manualEvent = pair.Value;

					Lock(pair.Key);

					lock (pair.Value)
					{
						if (!pair.Value.SafeWaitHandle.IsClosed)
							pair.Value.Set();
					}
				}
			}
		}

		#endregion
	}
}