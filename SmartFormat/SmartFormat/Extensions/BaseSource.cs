//
// Copyright (C) axuno gGmbH, Scott Rippey, Bernhard Millauer and other contributors.
// Licensed under the MIT license.
//

using SmartFormat.Core.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFormat.Extensions
{
	public abstract class BaseSource : ISource
	{
		protected BaseSource()
		{
		}

		ValueTask<bool> ISource.TryEvaluateSelectorAsync(ISelectorInfo selectorInfo, CancellationToken cancellationToken)
			=> new(TryEvaluateSelector(selectorInfo));

		public abstract bool TryEvaluateSelector(ISelectorInfo selectorInfo);
	}
}