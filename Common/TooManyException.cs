namespace Ecng.Common;

using System;

public class TooManyException(string message) : InvalidOperationException(message)
{
}