namespace Jiro.Core.Attributes;

/// <summary>
/// Attribute used to mark properties that should be anonymized or obfuscated in logs or output.
/// This attribute can be applied to properties to indicate they contain sensitive data.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AnomifyAttribute : Attribute
{

}
