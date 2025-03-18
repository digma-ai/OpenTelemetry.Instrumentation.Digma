using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;

public class VerticaInstrumentation
{
    private static readonly ActivitySource ActivitySource = new("OpenTelemetry.Instrumentation.Vertica");
    
    private static readonly MethodInfo PrefixMethodInfo = typeof(VerticaInstrumentation).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo FinalizerMethodInfo = typeof(VerticaInstrumentation).GetMethod(nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic);

    private static MethodInfo CommandTextGetter;
    private static MethodInfo ConnectionGetter;
    private static MethodInfo DatabaseGetter;

    private readonly Harmony _harmony;

    private readonly string[] TargetMethodNames =
    {
        "ExecuteReader",
        "ExecuteScalar",
        "ExecuteNonQuery",
    };

    public VerticaInstrumentation(Harmony harmony)
    {
        _harmony = harmony;
    }

    public void Instrument(Assembly verticaDataAssembly)
    {
        try
        {
            var verticaCommandType = verticaDataAssembly.GetType("Vertica.Data.VerticaClient.VerticaCommand", throwOnError: false);
            if (verticaCommandType == null)
            {
                Logger.LogError("Vertica.Data.VerticaClient.VerticaCommand not found.");
                return;
            }
            
            var sCommandType = verticaDataAssembly.GetType("Vertica.Data.Internal.ADO.Net.SCommand", throwOnError: false);
            if (sCommandType == null)
            {
                Logger.LogError("Vertica.Data.Internal.ADO.Net.SCommand not found.");
                return;
            }
            
            CommandTextGetter = sCommandType.GetProperty("CommandText")?.GetMethod;
            if (CommandTextGetter == null)
            {
                Logger.LogError("Vertica.Data.Internal.ADO.Net.SCommand.CommandText getter not found.");
                return;
            }

            ConnectionGetter = sCommandType.GetProperties()
                .FirstOrDefault(x => x.Name == "Connection" && x.DeclaringType == sCommandType)?.GetMethod;           
            if (ConnectionGetter == null)
            {
                Logger.LogError("Vertica.Data.Internal.ADO.Net.SCommand.Connection getter not found.");
                return;
            }
            
            var sConnectionType = verticaDataAssembly.GetType("Vertica.Data.Internal.ADO.Net.SConnection", throwOnError: false);
            if (sConnectionType == null)
            {
                Logger.LogError("Vertica.Data.Internal.ADO.Net.SConnection not found.");
                return;
            }
            DatabaseGetter = sConnectionType.GetProperty("Database")?.GetMethod;     
            if (ConnectionGetter == null)
            {
                Logger.LogError("Vertica.Data.Internal.ADO.Net.SConnection.Database getter not found.");
                return;
            }
            
            var methodInfos = verticaCommandType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => TargetMethodNames.Contains(x.Name))
                .Where(x => x.DeclaringType == verticaCommandType)
                .ToArray();
            foreach (var methodInfo in methodInfos)
            {
                _harmony.Patch(methodInfo, prefix: PrefixMethodInfo, finalizer: FinalizerMethodInfo);
                Logger.LogInfo($"Patched {methodInfo.FullDescription()}");
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to patch Vertica.Data.VerticaClient.VerticaCommand", e);
        }
    }
    
    private static void Prefix(MethodBase __originalMethod, object __instance, out Activity __state)
    {
        var connection = ConnectionGetter.Invoke(__instance, null);
        var database = DatabaseGetter.Invoke(connection, null)?.ToString();
        var sqlStatement = CommandTextGetter.Invoke(__instance, null);
        
        var activity = ActivitySource.StartActivity(database ?? __originalMethod.Name, ActivityKind.Client);
        Logger.LogDebug($"Opened Activity: {activity?.Source.Name}.{activity?.OperationName}");
        activity?.SetTag("db.statement", sqlStatement);
        activity?.SetTag("db.system", "vertica");
        activity?.SetTag("db.name", database);
        
        __state = activity;
    }

    private static void Finalizer(MethodBase __originalMethod, Activity __state, Exception __exception)
    {
        var activity = __state;
        if (activity == null) 
            return;
        
        if (__exception != null)
        {
            activity.RecordException(__exception);
            activity.SetStatus(ActivityStatusCode.Error);
        }            
        activity.Dispose();
        Logger.LogDebug($"Closed Activity: {activity.Source.Name}.{activity.OperationName}");
    }
}