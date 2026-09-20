namespace Ecng.Logging;

using System.Reflection;

/// <summary>
/// One remembered level, and the set it belongs to.
/// </summary>
/// <param name="set">The set the entry was made in; its identity is what dates the answer.</param>
/// <param name="level">The effective level.</param>
internal sealed class LogLevelEntry(LogLevelEntry[] set, LogLevels level)
{
	/// <summary>
	/// The set this entry was made in.
	/// </summary>
	public readonly LogLevelEntry[] Set = set;

	/// <summary>
	/// The effective level.
	/// </summary>
	public readonly LogLevels Level = level;
}

/// <summary>
/// One remembered starting point for a walk that cannot be shortened any further, and the set it
/// belongs to.
/// </summary>
/// <param name="set">The set the entry was made in.</param>
/// <param name="from">The link the walk has to start from.</param>
internal sealed class LogLevelWalk(LogLevelEntry[] set, ILogSource from)
{
	/// <summary>
	/// The set this entry was made in.
	/// </summary>
	public readonly LogLevelEntry[] Set = set;

	/// <summary>
	/// The link the walk has to start from, because from there on a level can move without the
	/// cache being told. Everything between the source holding this entry and that link is
	/// <see cref="LogLevels.Inherit"/> and cannot move without invalidating the set.
	/// </summary>
	public readonly ILogSource From = from;
}

/// <summary>
/// Shared invalidation stamp for the effective level cached by <see cref="BaseLogSource"/>.
/// </summary>
internal static class LogLevelCache
{
	// Levels run from zero without gaps, so an entry per level fits an array indexed by it.
	private static readonly int _levelCount = Enumerator.GetValues<LogLevels>().Max(l => (int)l) + 1;

	// A fresh set per invalidation, never a counter: a stamp taken before a change can never
	// compare equal to the current one again, so a stale entry cannot be mistaken for a fresh one
	// and no wrap-around can resurrect it. The entries are made once per invalidation and shared
	// by every source, so remembering a level allocates nothing and stores one reference - two
	// sources answering at once leave one whole entry or the other, never a level from one paired
	// with the stamp of another.
	private static LogLevelEntry[] _current = NewSet();

	private const BindingFlags _publicInstance = BindingFlags.Public | BindingFlags.Instance;

	// The two properties the walk reads. A remembered level stays valid only while every write to
	// them passes through BaseLogSource, which is the only place Invalidate is called from.
	private static readonly string[] _guardedAccessors =
	[
		$"get_{nameof(ILogSource.LogLevel)}",
		$"set_{nameof(ILogSource.LogLevel)}",
		$"get_{nameof(ILogSource.Parent)}",
		$"set_{nameof(ILogSource.Parent)}",
	];

	private static readonly SynchronizedDictionary<Type, bool> _cacheableTypes = new();

	/// <summary>
	/// The current set. An entry from it was computed from a chain that has not changed since.
	/// </summary>
	public static LogLevelEntry[] Current => Volatile.Read(ref _current);

	/// <summary>
	/// Invalidate every cached level.
	/// </summary>
	public static void Invalidate() => Volatile.Write(ref _current, NewSet());

	/// <summary>
	/// The entry standing for <paramref name="level"/> in <paramref name="set"/>.
	/// </summary>
	/// <param name="set">The set to take the entry from.</param>
	/// <param name="level">The effective level.</param>
	/// <returns>The entry.</returns>
	public static LogLevelEntry Entry(LogLevelEntry[] set, LogLevels level)
	{
		var index = (int)level;
		var entry = Volatile.Read(ref set[index]);

		if (entry is not null)
			return entry;

		// Two sources filling the same slot at once make two equal entries and one of them is
		// dropped; both name the same set and the same level, so either answers for either.
		entry = new(set, level);
		Volatile.Write(ref set[index], entry);

		return entry;
	}

	/// <summary>
	/// Whether a source of <paramref name="type"/> may cache its effective level.
	/// </summary>
	/// <param name="type">The runtime type of the source.</param>
	/// <returns><see langword="true"/> if the type keeps both guarded properties in <see cref="BaseLogSource"/>.</returns>
	public static bool IsCacheable(Type type)
		// Keying the memo by a type from a collectible assembly would pin that assembly and keep
		// its load context from ever unloading, so such a type is answered without remembering it.
		=> type.Assembly.IsCollectible ? Detect(type) : _cacheableTypes.SafeAdd(type, Detect);

	private static LogLevelEntry[] NewSet() => new LogLevelEntry[_levelCount];

	private static bool Detect(Type type)
	{
		try
		{
			// The interface map answers what ILogSource.LogLevel actually dispatches to for this
			// type, so an override, a shadowing member and an explicit re-implementation are all
			// caught by the same check.
			var map = type.GetInterfaceMap(typeof(ILogSource));

			for (var i = 0; i < map.InterfaceMethods.Length; i++)
			{
				if (!_guardedAccessors.Contains(map.InterfaceMethods[i].Name))
					continue;

				if (map.TargetMethods[i].DeclaringType != typeof(BaseLogSource))
					return false;
			}

			// And the declarations answer the same question for a caller holding the derived type
			// rather than the interface, which is how the property is usually set.
			var level = type.GetProperty(nameof(ILogSource.LogLevel), _publicInstance);
			var parent = type.GetProperty(nameof(ILogSource.Parent), _publicInstance);

			return IsOwn(level?.GetGetMethod()) && IsOwn(level?.GetSetMethod())
				&& IsOwn(parent?.GetGetMethod()) && IsOwn(parent?.GetSetMethod());
		}
		catch (Exception)
		{
			// A shape reflection cannot describe is an unknown one: walk it instead of caching it.
			return false;
		}
	}

	private static bool IsOwn(MethodInfo accessor) => accessor?.DeclaringType == typeof(BaseLogSource);
}
