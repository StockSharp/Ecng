namespace Ecng.Common;

using System;

public class ExpiredException(string message) : InvalidOperationException(message)
{
}