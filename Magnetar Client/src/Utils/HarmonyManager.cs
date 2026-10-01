using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Magnetar_Client.Utils;

public struct HarmonyPatchFailure
{
    public string ClassName { get; }
    public Type TargetType { get; }
    public Exception Exception { get; }

    public HarmonyPatchFailure(string className, Type targetType, Exception exception)
    {
        ClassName = className;
        TargetType = targetType;
        Exception = exception;
    }
}

public struct HarmonyPatchInfo
{
    public int SuccessCount { get; }
    public int FailCount { get; }
    public int TotalClassesEvaluated { get; }
    public IReadOnlyList<string> FailedClassNames { get; }
    public IReadOnlyList<Exception> Exceptions { get; }
    public IReadOnlyList<HarmonyPatchFailure> Failures { get; }

    public bool HasFailures => FailCount > 0;

    public HarmonyPatchInfo(
        int successCount,
        int failCount,
        int totalEvaluated,
        List<string> failedClassNames,
        List<Exception> exceptions,
        List<HarmonyPatchFailure> failures)
    {
        SuccessCount = successCount;
        FailCount = failCount;
        TotalClassesEvaluated = totalEvaluated;
        FailedClassNames = failedClassNames.AsReadOnly();
        Exceptions = exceptions.AsReadOnly();
        Failures = failures.AsReadOnly();
    }
}

public static class HarmonyManager
{
    /// <summary>
    /// Applies all Harmony patches from the specified assembly and returns diagnostic patch info with recorded exceptions.
    /// </summary>
    public static HarmonyPatchInfo HarmonyPatchAll(Assembly assembly, HarmonyLib.Harmony harmony)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        if (harmony == null) throw new ArgumentNullException(nameof(harmony));

        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null).ToArray();
        }

        int successCount = 0;
        int failCount = 0;
        int evaluatedCount = 0;
        List<string> failedClassNames = new();
        List<Exception> exceptions = new();
        List<HarmonyPatchFailure> failures = new();

        foreach (var type in types)
        {
            if (type == null) continue;
            evaluatedCount++;

            try
            {
                var patchedMethods = harmony.CreateClassProcessor(type).Patch();
                if (patchedMethods != null && patchedMethods.Count > 0)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                string className = type.FullName ?? type.Name;
                failedClassNames.Add(className);
                exceptions.Add(ex);
                failures.Add(new HarmonyPatchFailure(className, type, ex));
                failCount++;
            }
        }

        return new HarmonyPatchInfo(
            successCount,
            failCount,
            evaluatedCount,
            failedClassNames,
            exceptions,
            failures
        );
    }
}