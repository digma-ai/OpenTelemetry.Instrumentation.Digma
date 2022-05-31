namespace OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;

/// <summary>
/// An Attribute 
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class ActivitiesAttributesAttribute : Attribute
{
    public IDictionary<string, string> Attributes { get; }
    /// <summary>
    /// Define a set of attributes that will be included with every Activity
    /// created by the TracingDecorator for this object
    /// Example usage:
    /// 
    /// ActivitiesAttribute("some_attribute:some_value", "someother_attribute, "some_other_value")
    /// 
    /// </summary>
    /// <param name="extraAttributes">A set of attributes defined as "key:value"</param>
    public ActivitiesAttributesAttribute(params string[] extraAttributes)
    {
        Attributes =TraceAttributesInputsFormat.ActivityAttributesStringsToDictionary(extraAttributes);

    }
}

internal static class TraceAttributesInputsFormat
{
    internal static IDictionary<string, string> ActivityAttributesStringsToDictionary(params string[] attributes)
    {
        if (!attributes.Any())
        {
            return new Dictionary<string, string>();
        }
        var attributeFragements = attributes
            .Select(x => x.Split(":")).ToArray();
        
        EnsureAttributeSyntax(attributes, attributeFragements);

        EnsureUniqueKeys(attributes, attributeFragements);

        return attributeFragements.ToDictionary(x => x[0], x => x[1]);
        
    }

    private static void EnsureUniqueKeys(string[] attributes, string[][] attributeFragements)
    {
        var attributeKeys = attributeFragements.Select(x => x[0]).ToArray();
        if (attributeKeys.Length != attributeKeys.Distinct().ToArray().Length)
        {
            throw new ArgumentException($"Attribute keys must be unique. Provided value was: " +
                                        $"{String.Join(',', attributes)}");
        }
    }

    private static void EnsureAttributeSyntax(string[] attributes, string[][] attributeFragements)
    {
        var illegalFragments = attributeFragements.Where(x => x.Length != 2);
        if (illegalFragments.Any())
        {
            throw new ArgumentException(
                $"Illegal attribute values provided in value:{attributes}" +
                " The correct syntax is \"key:value\"");
        }
    }
}
