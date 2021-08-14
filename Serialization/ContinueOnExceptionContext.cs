namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public class ContinueOnExceptionContext
	{
		public event Action<Exception> Error;

		public bool DoNotEncrypt { get; set; }

		public static bool TryProcess(Exception ex)
		{
			if (ex is null)
				throw new ArgumentNullException(nameof(ex));

			var ctx = Scope<ContinueOnExceptionContext>.Current;

			if (ctx is null)
				return false;

			ctx.Value.Process(ex);
			return true;
		}

		public void Process(Exception ex)
		{
			if (ex is null)
				throw new ArgumentNullException(nameof(ex));

			Error?.Invoke(ex);
		}
	}
}