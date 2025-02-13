using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace OpenTelemetry.AutoInstrumentation.Digma;

public class Plugin
{
    public void Initializing()
    {
        Logger.Log("Initializing started");
        AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
        {
            var assemblyName = args.LoadedAssembly.GetName().Name;
            Logger.Log($"Initializing AssemblyLoad {assemblyName}");
            if (assemblyName == "System.Data")
            {
                Patch(args.LoadedAssembly);
            }
        };
        Logger.Log("Initializing ended");
    }

    public void Patch(Assembly systemDataAssembly)
    {
        Logger.Log("Patching started");
        var harmony = new Harmony("SqlClientPatch");
        // harmony.PatchAll();

        var sqlCommandType = systemDataAssembly.GetType("System.Data.SqlClient.SqlCommand", throwOnError: false);
        if (sqlCommandType == null)
        {
            Logger.Log("System.Data.SqlClient.SqlCommand not found.");
            return;
        }

        var targetMethodInfo = sqlCommandType.GetMethod("WriteBeginExecuteEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        if (targetMethodInfo == null)
        {
            Logger.Log("WriteBeginExecuteEvent not found.");
            return;
        }
        
        harmony.Patch(targetMethodInfo, transpiler: new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)));
        Logger.Log("Patching applied successfully.");
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = new List<CodeInstruction>(instructions);

        Logger.Log($"Before ({instructionList.Count}):\n"+ 
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
        
        Logger.Log($"After ({instructionList.Count}):\n"+ 
                   string.Join("\n",instructionList.Select(x => x.ToString()).ToArray()));

        return instructionList;
    }
}