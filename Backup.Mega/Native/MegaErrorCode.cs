namespace Ecng.Backup.Mega.Native;

/// <summary>
/// MEGA API error codes (negative integers) returned as JSON numbers.
/// </summary>
public enum MegaErrorCode
{
	Ok = 0,
	Unknown = int.MinValue,

	/// <summary>Internal error.</summary>
	Internal = -1,

	/// <summary>Bad arguments.</summary>
	BadArguments = -2,

	/// <summary>Request failed, retry with exponential backoff.</summary>
	Again = -3,

	/// <summary>Too many requests, slow down.</summary>
	RateLimit = -4,

	/// <summary>Request failed permanently.</summary>
	Failed = -5,

	/// <summary>Too many requests for this resource.</summary>
	TooMany = -6,

	/// <summary>Resource access out of range.</summary>
	Range = -7,

	/// <summary>Resource expired.</summary>
	Expired = -8,

	/// <summary>Resource does not exist.</summary>
	NotFound = -9,

	/// <summary>Circular linkage.</summary>
	Circular = -10,

	/// <summary>Access denied.</summary>
	Access = -11,

	/// <summary>Resource already exists.</summary>
	Exists = -12,

	/// <summary>Request incomplete.</summary>
	Incomplete = -13,

	/// <summary>Cryptographic error.</summary>
	Key = -14,

	/// <summary>Bad session ID.</summary>
	BadSession = -15,

	/// <summary>Resource administratively blocked.</summary>
	Blocked = -16,

	/// <summary>Quota exceeded.</summary>
	OverQuota = -17,

	/// <summary>Resource temporarily not available.</summary>
	TempUnavailable = -18,

	/// <summary>Too many connections on this resource.</summary>
	TooManyConnections = -19,

	/// <summary>File could not be written (or failed post-write integrity check).</summary>
	Write = -20,

	/// <summary>File could not be read (or changed unexpectedly during reading).</summary>
	Read = -21,

	/// <summary>Invalid or missing application key.</summary>
	AppKey = -22,

	/// <summary>SSL verification failed.</summary>
	Ssl = -23,

	/// <summary>Not enough quota.</summary>
	GoingOverQuota = -24,

	/// <summary>A strongly-grouped request was rolled back.</summary>
	RolledBack = -25,

	/// <summary>Multi-factor authentication required.</summary>
	MfaRequired = -26,

	/// <summary>Access denied for sub-users (business accounts).</summary>
	MasterOnly = -27,

	/// <summary>MEGA API also uses -27 in a special array response to request hashcash; see client parser.</summary>
	HashcashRequired = -27,

	/// <summary>Business account expired.</summary>
	BusinessPastDue = -28,

	/// <summary>Over Disk Quota Paywall.</summary>
	Paywall = -29,

	/// <summary>Subuser has not yet encrypted their master key for the admin user.</summary>
	SubUserKeyMissing = -30,

	LocalNoSpace = -1000,
	LocalTimeout = -1001,
	LocalAbandoned = -1002,
	LocalNetwork = -1003,
	LocalLoggedOut = -1004,

	FuseBadFileDescriptor = -2000,
	FuseIsDirectory = -2001,
	FuseNameTooLong = -2002,
	FuseNotDirectory = -2003,
	FuseNotEmpty = -2004,
	FuseNotFound = -2005,
	FusePermission = -2006,
	FuseReadOnlyFileSystem = -2007,
	FuseAlready = -2008,
	FuseCancelled = -2009,
	FuseDuplicate = -2010,
}
