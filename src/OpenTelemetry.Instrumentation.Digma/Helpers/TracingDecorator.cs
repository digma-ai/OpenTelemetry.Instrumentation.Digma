using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.Digma.Helpers;

public class TraceDecorator<TDecorated> : DispatchProxy where TDecorated : class
{
    private ActivitySource _activity;
    private TDecorated _decorated;
    private IActivityNamingSchema _namingSchema = new MethodNameSchema();
    private bool _decorateAllMethods = true;

    private readonly ConcurrentDictionary<string, NoActivityAttribute?> _methodNoActivityAttributeCache = new();
    private readonly ConcurrentDictionary<string, ActivitiesAttributesAttribute?> _methodActivitiesTagsCache = new();
    private readonly ConcurrentDictionary<string, TraceActivityAttribute?> _methodActivityAttributeCache = new();
    /// <summary>
    /// Creates a new TraceDecorator instance wrapping the specific object and implementing the TDecorated interface 
    /// </summary>
    /// <param name="decorated"></param>
    /// <param name="activityNamingSchema"></param>
    /// <param name="decorateAllMethods"></param>
    /// <returns></returns>
    public static TDecorated Create(TDecorated decorated, IActivityNamingSchema? activityNamingSchema = null,
        bool decorateAllMethods = true)
    {
        object proxy = Create<TDecorated, TraceDecorator<TDecorated>>()!;
        ((TraceDecorator<TDecorated>)proxy!).SetParameters(decorated, activityNamingSchema, decorateAllMethods);

        return (TDecorated)proxy;
    }

    private void SetParameters(TDecorated decorated, IActivityNamingSchema? spanNamingSchema, bool decorateAllMethods)
    {
        _decorated = decorated;
        _activity = new(_decorated!.GetType().FullName!);
        _decorateAllMethods = decorateAllMethods;
        if (spanNamingSchema != null)
        {
            _namingSchema = spanNamingSchema;
        }
    }

    private bool IsAsync(MethodInfo targetMethod)
    {
        var taskType = typeof(Task);
        return targetMethod.ReturnType == taskType ||
               targetMethod.ReturnType.IsSubclassOf(taskType);
    }
    private object? InvokeAsyncDecoratedExecution(Activity? activity, MethodInfo? targetMethod, object?[]? args,
        bool? recordException)
    {
        activity.Start();
        var resultTask = targetMethod.Invoke(_decorated, args) as Task;
        
        resultTask.ContinueWith(task =>
        {
            if (task.Exception != null && (recordException ?? true))
            {
                activity?.RecordException(task.Exception);
            }

            activity?.Stop();

            activity?.Dispose();


        },TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

        return resultTask;
    }

    private object? InvokeDecoratedExecution(Activity? activity, MethodInfo? targetMethod, object?[]? args,
        bool? recordException)
    {
        
        object? result;
        try
        {
            using (activity)
            {
                result = targetMethod.Invoke(_decorated, args);
            }
        }
        catch (Exception e)
        {
            if (recordException ?? true)
                activity?.RecordException(e);

            throw;
        }
        

        return result;



    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod != null)
        {
            
            var noActivityAttribute = GetMethodNoActivityAttribute(targetMethod);
            var activityAttribute = GetMethodActivityAttribute(targetMethod);

            if (noActivityAttribute == null && _decorateAllMethods )
            {
                var classType = _decorated!.GetType();

                var defaultSpanName = _namingSchema.GetSpanName(classType, targetMethod);
                var activity = _activity.StartActivity(activityAttribute?.Name ?? defaultSpanName);

                SpanUtils.AddCommonTags(classType,targetMethod, activity);
                InjectAttributes(targetMethod, activity);

                if (IsAsync(targetMethod))
                {
                    return InvokeAsyncDecoratedExecution(activity, targetMethod, args,
                        activityAttribute?.RecordExceptions);
                }
                
                return InvokeDecoratedExecution(activity, targetMethod, args, activityAttribute?.RecordExceptions);
            }
            

        }

        return InvokeDecoratedExecution(null, targetMethod, args, null);
    }

    private TAttribute? GetInstanceMethodAttribute<TAttribute>(MethodInfo targetMethod) where TAttribute : Attribute
    {
        return _decorated.GetType().GetMethod(targetMethod.Name)?.GetCustomAttribute<TAttribute>(inherit:true);
    }


    private TAttribute? GetFromOrStoreInAttributeCache<TAttribute>(
        MethodInfo targetMethod,
        IDictionary<string, TAttribute?> cache) where TAttribute : Attribute
    {
        TAttribute? attributeInfo;

        if (!cache.TryGetValue(targetMethod.Name, out attributeInfo))

        {
            attributeInfo = GetInstanceMethodAttribute<TAttribute>(targetMethod);
            cache[targetMethod.Name] = attributeInfo;
            
        }
        
        return attributeInfo;
    }
    private ActivitiesAttributesAttribute? GetActivityTagsForInstanceMethod(MethodInfo targetMethod)
    {
        return GetFromOrStoreInAttributeCache(targetMethod, _methodActivitiesTagsCache);
    }
    
    private TraceActivityAttribute? GetMethodActivityAttribute(MethodInfo targetMethod)
    {
        return GetFromOrStoreInAttributeCache(targetMethod, _methodActivityAttributeCache);
    }
    
    private NoActivityAttribute? GetMethodNoActivityAttribute(MethodInfo targetMethod)
    {
        return GetFromOrStoreInAttributeCache(targetMethod, _methodNoActivityAttributeCache);
    }

    private void InjectAttributes(MethodInfo targetMethod, Activity? activity)
    {
        var methodActivityAttributes = GetActivityTagsForInstanceMethod(targetMethod);
        var classActivityAttributes =
            _decorated.GetType().GetCustomAttribute<ActivitiesAttributesAttribute>(inherit: false);

        if (methodActivityAttributes != null)
        {
            foreach (var key in methodActivityAttributes.Attributes.Keys)
            {
                activity.AddTag(key, methodActivityAttributes.Attributes[key]);
            }
        }

        if (classActivityAttributes != null)
        {
            foreach (var key in classActivityAttributes.Attributes.Keys)
            {
                activity.AddTag(key, classActivityAttributes.Attributes[key]);
            }
        }
    }
}