namespace Ecng.Common;

/// <summary>
/// A lazy initialization wrapper that supports resetting.
/// </summary>
/// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
public class ResettableLazy<T>
{
	private readonly Func<T> _valueFactory;
	private readonly LazyThreadSafetyMode _mode;
	private readonly Lock _lock;

	private T _value;
	private volatile bool _isValueCreated;

	/// <summary>
	/// Initializes a new instance of the <see cref="ResettableLazy{T}"/> class.
	/// </summary>
	/// <param name="valueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
	public ResettableLazy(Func<T> valueFactory)
		: this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ResettableLazy{T}"/> class.
	/// </summary>
	/// <param name="valueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
	/// <param name="mode">The lazy thread-safety mode.</param>
	public ResettableLazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
	{
		_valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
		_mode = mode;

		if (mode != LazyThreadSafetyMode.None)
			_lock = new Lock();
	}

	/// <summary>
	/// Gets a value that indicates whether a value has been created for this instance.
	/// </summary>
	public bool IsValueCreated => _isValueCreated;

	/// <summary>
	/// Gets the lazily initialized value of the current instance.
	/// </summary>
	public T Value
	{
		get
		{
			if (_isValueCreated)
			{
				if (_mode == LazyThreadSafetyMode.None)
					return _value;

				// Read under the lock so a concurrent Reset/SetValue cannot make us observe a
				// half-updated state - e.g. the flag still set while _value has been cleared back
				// to default. Re-check the flag inside the lock; if it was reset, fall through.
				using (_lock.EnterScope())
				{
					if (_isValueCreated)
						return _value;
				}
			}

			return CreateValue();
		}
	}

	private T CreateValue()
	{
		switch (_mode)
		{
			case LazyThreadSafetyMode.None:
				_value = _valueFactory();
				_isValueCreated = true;
				return _value;

			case LazyThreadSafetyMode.PublicationOnly:
			{
				var value = _valueFactory();

				using (_lock.EnterScope())
				{
					if (!_isValueCreated)
					{
						_value = value;
						_isValueCreated = true;
					}

					// Return the winning value from inside the lock so a Reset racing the
					// lock release can't turn this into a default(T).
					return _value;
				}
			}

			case LazyThreadSafetyMode.ExecutionAndPublication:
			default:
			{
				using (_lock.EnterScope())
				{
					if (!_isValueCreated)
					{
						_value = _valueFactory();
						_isValueCreated = true;
					}

					return _value;
				}
			}
		}
	}

	/// <summary>
	/// Resets the lazy initialization, allowing the value to be recreated on next access.
	/// </summary>
	public void Reset()
	{
		if (_mode == LazyThreadSafetyMode.None)
		{
			_value = default;
			_isValueCreated = false;
		}
		else
		{
			using (_lock.EnterScope())
			{
				_value = default;
				_isValueCreated = false;
			}
		}
	}

	/// <summary>
	/// Sets the value directly without calling the factory.
	/// </summary>
	/// <param name="value">The value to set.</param>
	public void SetValue(T value)
	{
		if (_mode == LazyThreadSafetyMode.None)
		{
			_value = value;
			_isValueCreated = true;
		}
		else
		{
			using (_lock.EnterScope())
			{
				_value = value;
				_isValueCreated = true;
			}
		}
	}

	/// <summary>
	/// Implicitly converts to the underlying value.
	/// </summary>
	public static implicit operator T(ResettableLazy<T> lazy)
		=> lazy is null ? default : lazy.Value;

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	public override string ToString()
		=> _isValueCreated ? (_value?.ToString() ?? string.Empty) : "<not initialized>";
}