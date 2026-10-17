using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Magnetar_Client.Api;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules;

public partial class DebugMode
{
    private static void DumpEventNodes()
    {
        try
        {
            string outDir = PathsManager.DebugModeDir;
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string filePath = Path.Combine(outDir, "EventNodes_Definitions.txt");
            StringBuilder sb = new();

            sb.AppendLine("==================================================");
            sb.AppendLine("           EVENT NODES REFLECTION DUMP            ");
            sb.AppendLine($"           Generated: {DateTime.Now}             ");
            sb.AppendLine("==================================================");
            sb.AppendLine();

            Assembly targetAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetType("Il2CppGameLevel.EventNodes.EventNodeBase") != null)
                {
                    targetAssembly = asm;
                    break;
                }
            }

            if (targetAssembly == null)
            {
                DebugModeLogger.Error("Could not locate assembly containing 'Il2CppGameLevel.EventNodes.EventNodeBase'.");
                return;
            }

            Type eventNodeBaseType = targetAssembly.GetType("Il2CppGameLevel.EventNodes.EventNodeBase");
            Type eventSuggestedNodeType = targetAssembly.GetType("Il2CppGameLevel.EventNodes.EventSuggestedNode");

            Type[] allTypes;
            try
            {
                allTypes = targetAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                allTypes = ex.Types.Where(t => t != null).ToArray();
            }

            var nodeTypes = allTypes
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.Namespace == "Il2CppGameLevel.EventNodes" &&
                            eventNodeBaseType.IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            int count = 0;
            foreach (Type type in nodeTypes)
            {
                count++;
                bool isSuggestedNode = eventSuggestedNodeType != null && eventSuggestedNodeType.IsAssignableFrom(type);

                sb.AppendLine($"[Node Class]: {type.FullName}");
                sb.AppendLine($"  Type Kind: {(isSuggestedNode ? "EventSuggestedNode" : "EventNodeBase")}");

                // 1. Create instance to read instance properties/fields
                object instance = null;
                try
                {
                    ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        instance = Activator.CreateInstance(type);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  [Warning: Could not create instance: {ex.Message}]");
                }

                sb.AppendLine("  -> String Members:");
                bool hasStringMember = false;

                // 2. Dump all string Properties (excluding IL2CPP native pointer markers)
                var stringProps = type.GetProperties(allFlags)
                    .Where(p => p.PropertyType == typeof(string) &&
                                !p.Name.StartsWith("NativeFieldInfoPtr_", StringComparison.OrdinalIgnoreCase) &&
                                !p.Name.StartsWith("NativeMethodInfoPtr_", StringComparison.OrdinalIgnoreCase));

                foreach (PropertyInfo prop in stringProps)
                {
                    hasStringMember = true;
                    string valStr = "<unreadable>";
                    try
                    {
                        MethodInfo getter = prop.GetGetMethod(true);
                        if (getter != null)
                        {
                            object val = getter.IsStatic ? prop.GetValue(null) : (instance != null ? prop.GetValue(instance) : "<no instance>");
                            valStr = val != null ? $"\"{val}\"" : "null";
                        }
                    }
                    catch (Exception ex)
                    {
                        valStr = $"<error: {ex.Message}>";
                    }

                    MethodInfo getMethod = prop.GetGetMethod(true);
                    bool isStatic = getMethod != null && getMethod.IsStatic;
                    sb.AppendLine($"     - [Property] {(isStatic ? "[static] " : "")}{prop.Name} = {valStr}");
                }

                // 3. Dump all string Fields
                var stringFields = type.GetFields(allFlags)
                    .Where(f => f.FieldType == typeof(string) &&
                                !f.Name.StartsWith("NativeFieldInfoPtr_", StringComparison.OrdinalIgnoreCase) &&
                                !f.Name.StartsWith("NativeMethodInfoPtr_", StringComparison.OrdinalIgnoreCase));

                foreach (FieldInfo field in stringFields)
                {
                    hasStringMember = true;
                    string valStr = "<unreadable>";
                    try
                    {
                        object val = field.IsStatic ? field.GetValue(null) : (instance != null ? field.GetValue(instance) : "<no instance>");
                        valStr = val != null ? $"\"{val}\"" : "null";
                    }
                    catch (Exception ex)
                    {
                        valStr = $"<error: {ex.Message}>";
                    }

                    sb.AppendLine($"     - [Field] {(field.IsStatic ? "[static] " : "")}{field.Name} = {valStr}");
                }

                if (!hasStringMember)
                {
                    sb.AppendLine("     (none)");
                }

                // 4. Dump port definitions if it's an EventSuggestedNode
                if (isSuggestedNode)
                {
                    sb.AppendLine("  -> Suggested Node Port Definitions:");
                    DumpStaticPortDefinition(type, "inputs", allFlags, sb);
                    DumpStaticPortDefinition(type, "outputs", allFlags, sb);
                }

                sb.AppendLine(new string('-', 50));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            DebugModeLogger.Msg($"Successfully dumped {count} EventNode classes to: '{filePath}'");
        }
        catch (Exception ex)
        {
            DebugModeLogger.Error($"Failed dumping event nodes: {ex}");
        }
    }

    private static void DumpStaticPortDefinition(Type type, string memberName, BindingFlags flags, StringBuilder sb)
    {
        object arrayObj = null;

        FieldInfo field = type.GetField(memberName, flags);
        if (field != null && field.IsStatic)
        {
            try { arrayObj = field.GetValue(null); } catch { }
        }

        if (arrayObj == null)
        {
            PropertyInfo prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead)
            {
                MethodInfo getter = prop.GetGetMethod(true);
                if (getter != null && getter.IsStatic)
                {
                    try { arrayObj = prop.GetValue(null); } catch { }
                }
            }
        }

        if (arrayObj == null)
        {
            sb.AppendLine($"     - {memberName}: (none / null)");
            return;
        }

        if (arrayObj is IEnumerable enumerable)
        {
            sb.AppendLine($"     - {memberName}:");
            int idx = 0;
            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    sb.AppendLine($"        [{idx++}] null");
                    continue;
                }

                Type itemType = item.GetType();
                string portName = GetMemberValue(item, itemType, "portName");
                string portType = GetMemberValue(item, itemType, "portType");
                string direction = GetMemberValue(item, itemType, "direction");

                sb.AppendLine($"        [{idx++}] Name: \"{portName}\", Type: {portType}, Direction: {direction}");
            }

            if (idx == 0)
            {
                sb.AppendLine($"        (empty array)");
            }
        }
        else
        {
            sb.AppendLine($"     - {memberName}: {arrayObj}");
        }
    }

    private static string GetMemberValue(object target, Type type, string name)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        PropertyInfo prop = type.GetProperty(name, flags);
        if (prop != null && prop.CanRead)
        {
            try
            {
                var val = prop.GetValue(target);
                return val?.ToString() ?? "null";
            }
            catch { }
        }

        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            try
            {
                var val = field.GetValue(target);
                return val?.ToString() ?? "null";
            }
            catch { }
        }

        return "unknown";
    }
}