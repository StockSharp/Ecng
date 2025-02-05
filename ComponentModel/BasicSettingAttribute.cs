namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Marks property as basic to be included into basic settings UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BasicSettingAttribute : Attribute
{
}