using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;

public class SqlClientInstrumentation
{
    private static readonly MethodInfo SqlTranspilerMethodInfo = typeof(SqlClientInstrumentation).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);

    private readonly Harmony _harmony;

    public SqlClientInstrumentation(Harmony harmony)
    {
        _harmony = harmony;
    }

    public void Instrument(Assembly systemDataAssembly)
    {
        try
        {
            var sqlCommandType = systemDataAssembly.GetType("System.Data.SqlClient.SqlCommand", throwOnError: false);
            if (sqlCommandType == null)
            {
                Logger.LogError("System.Data.SqlClient.SqlCommand not found.");
                return;
            }

            var targetMethodInfo =
                sqlCommandType.GetMethod("WriteBeginExecuteEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (targetMethodInfo == null)
            {
                Logger.LogError("WriteBeginExecuteEvent not found.");
                return;
            }

            _harmony.Patch(targetMethodInfo, transpiler: new HarmonyMethod(SqlTranspilerMethodInfo));
            Logger.LogInfo("Patched SqlClient");
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to patch System.Data.SqlClient.SqlCommand.WriteBeginExecuteEvent", e);
        }
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = new List<CodeInstruction>(instructions);

        Logger.LogDebug($"Before ({instructionList.Count}):\n"+ 
                        string.Join("\n",instructionList.Select(x => x.ToString()).ToArray()));

        for (var i = 0; i < instructionList.Count-1; i++)
        {
            if (instructionList[i].opcode == OpCodes.Ldarg_0 && // "this"
                instructionList[i+1].operand.ToString().Contains("System.Data.CommandType get_CommandType()"))
            {
                instructionList.RemoveRange(i, 6); 
                break;
            }
        }
        
        Logger.LogDebug($"After ({instructionList.Count}):\n"+ 
                        string.Join("\n",instructionList.Select(x => x.ToString()).ToArray()));

        return instructionList;
    }
}