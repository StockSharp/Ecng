namespace NuGet
{
    public interface IPropertyProvider
    {
        object GetPropertyValue(string propertyName);
    }
}
