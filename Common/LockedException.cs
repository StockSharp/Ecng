namespace Ecng.Common;

using System;

public class LockedException(string message) : InvalidOperationException(message)
{
}
