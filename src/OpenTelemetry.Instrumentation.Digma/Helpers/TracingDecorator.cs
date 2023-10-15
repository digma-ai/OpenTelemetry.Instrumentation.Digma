using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.Digma.Helpers
{
    public class TraceDecorator<TDecorated> : DispatchProxy where TDecorated : class
    {
        private ActivitySource _activity;
        private TDecorated _decorated;
        private IActivityNamingSchema _namingSchema = new MethodNameSchema();
        private bool _decorateAllMethods = true;

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
            ((TraceDecorator<TDecorated>) proxy!).SetParameters(decorated, activityNamingSchema, decorateAllMethods);

            return (TDecorated) proxy;
        }

        private void SetParameters(TDecorated decorated, IActivityNamingSchema? spanNamingSchema,
            bool decorateAllMethods)
        {
            _decorated = decorated;
            _activity = new ActivitySource(_decorated!.GetType().FullName!);
            _decorateAllMethods = decorateAllMethods;
            if (spanNamingSchema != null)
            {
                _namingSchema = spanNamingSchema;
            }
        }

        private object? InvokeDecoratedExecution(Activity? activity, MethodInfo? targetMethod, object?[]? args,
            bool? recordException)
        {
            object? result;
            try
            {
                result = targetMethod.Invoke(_decorated, args);
            }
            catch (Exception e)
            {
                if (recordException ?? true)
                    activity?.RecordException(e);

                activity?.Dispose();
                throw;
            }

            if (result is Task resultTask)
            {
                resultTask.ContinueWith(task =>
                {
                    if (task.Exception != null && (recordException ?? true))
                    {
                        activity?.RecordException(task.Exception);
                    }

                    activity?.Dispose();
                }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

                return result;
            }

            activity?.Dispose();
            return result;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var noActivityAttribute = targetMethod.GetCustomAttribute<NoActivityAttribute>(false);
            var activityAttribute = targetMethod.GetCustomAttribute<TraceActivityAttribute>(false);

            if (noActivityAttribute == null && (_decorateAllMethods || activityAttribute != null))
            {
                var classType = _decorated!.GetType();

                var defaultSpanName = _namingSchema.GetSpanName(classType, targetMethod);
                var activity = _activity.StartActivity(activityAttribute?.Name ?? defaultSpanName);

                SpanUtils.AddCommonTags(classType, targetMethod, activity);
                InjectAttributes(targetMethod, activity);

                return InvokeDecoratedExecution(activity, targetMethod, args, activityAttribute?.RecordExceptions);
            }

            return InvokeDecoratedExecution(null, targetMethod, args, null);
        }

        private void InjectAttributes(MethodInfo targetMethod, Activity? activity)
        {
            var methodActivityAttributes =
                targetMethod.GetCustomAttribute<ActivitiesAttributesAttribute>(inherit: false);
            var classActivityAttributes =
                _decorated.GetType().GetCustomAttribute<ActivitiesAttributesAttribute>(inherit: false);

            if (methodActivityAttributes != null)
            {
                foreach (var key in classActivityAttributes.Attributes.Keys)
                {
                    activity.AddTag(key, methodActivityAttributes.Attributes[key]);
                }
            }

            if (classActivityAttributes != null)
            {
                foreach (var key in classActivityAttributes.Attributes.Keys)
                {
                    activity.AddTag(key, methodActivityAttributes.Attributes[key]);
                }
            }
        }
    }
}