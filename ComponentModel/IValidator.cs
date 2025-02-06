namespace Ecng.ComponentModel;

public interface IValidator
{
	bool IsValid(object value, bool checkOnNull);
}