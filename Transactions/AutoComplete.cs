namespace Ecng.Transactions
{
	using System;
	using System.Transactions;

	public static class AutoComplete
	{
		public static void Do(Action action)
		{
			Do(new TransactionScope(), action);
		}

		public static void Do(TransactionScope scope, Action action)
		{
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			using (scope)
			{
				action();
				scope.Complete();
			}
		}
	}
}