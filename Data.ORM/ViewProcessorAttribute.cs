namespace Ecng.Serialization;

[AttributeUsage(ReflectionHelper.Types, Inherited = false)]
public class ViewProcessorAttribute(Type processorType) : Attribute
{
	public Type ProcessorType { get; } = processorType ?? throw new ArgumentNullException(nameof(processorType));
}
