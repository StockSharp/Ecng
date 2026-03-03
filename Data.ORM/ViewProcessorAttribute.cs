namespace Ecng.Serialization;

/// <summary>
/// Associates a view entity type with its <see cref="IViewProcessor"/> implementation.
/// </summary>
/// <param name="processorType">The type implementing <see cref="IViewProcessor"/>.</param>
[AttributeUsage(ReflectionHelper.Types, Inherited = false)]
public class ViewProcessorAttribute(Type processorType) : Attribute
{
	/// <summary>
	/// Gets the <see cref="IViewProcessor"/> implementation type.
	/// </summary>
	public Type ProcessorType { get; } = processorType ?? throw new ArgumentNullException(nameof(processorType));
}
