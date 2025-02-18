namespace Ecng.Backup.Mega.Native
{
  using System;
  using System.Net;

  interface ISession
  {
    string Client { get; }

    IPAddress IpAddress { get; }

    string Country { get; }

    DateTime LoginTime { get; }

    DateTime LastSeenTime { get; }

    SessionStatus Status { get; }

    string SessionId { get; }
  }

  [Flags]
  enum SessionStatus
  {
    Undefined = 0,
    Current = 1 << 0,
    Active = 1 << 1,
    Expired = 1 << 2
  }
}
