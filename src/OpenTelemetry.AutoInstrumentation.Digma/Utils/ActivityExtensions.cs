using System;
using System.Diagnostics;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

static class ActivityExtensions
{
    public static void SetCodeTags(this Activity activity, MethodBase method)
    {
        activity?.SetTag(DigmaSemanticConventions.CodeNamespace, method.DeclaringType?.FullName);
        activity?.SetTag(DigmaSemanticConventions.CodeFunction, method.Name);
    }
    
    // Taken from OpenTelemetry.Api
    public static void RecordException(this Activity activity, Exception ex)
    {
        if (ex == null || activity == null)
        {
            return;
        }

        var tagsCollection = new ActivityTagsCollection
        {
            { SemanticConventions.AttributeExceptionType, ex.GetType().FullName },
            { SemanticConventions.AttributeExceptionStacktrace, ex.ToString() },
        };

        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            tagsCollection.Add(SemanticConventions.AttributeExceptionMessage, ex.Message);
        }

        activity.AddEvent(new ActivityEvent(SemanticConventions.AttributeExceptionEventName, default, tagsCollection));
    }
}