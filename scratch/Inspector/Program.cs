using System;
using System.Reflection;
using System.Linq;

public class Inspector
{
    public static void Main()
    {
        try
        {
            var assembly = Assembly.LoadFrom("D:\\LethalMod\\OverseerProtocol\\references\\game\\Assembly-CSharp.dll");
            
            InspectType(assembly, "Item");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    static void InspectType(Assembly assembly, string typeName)
    {
        Console.WriteLine($"\n--- Inspecting {typeName} ---");
        var type = assembly.GetType(typeName);
        if (type == null) {
            Console.WriteLine("Type Not Found");
            return;
        }

        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            try {
                Console.WriteLine($"Field: {f.Name} (Type: {f.FieldType.Name})");
            } catch (Exception ex) {
                Console.WriteLine($"Field: {f.Name} (Error reading type: {ex.Message})");
            }
        }
    }
}
