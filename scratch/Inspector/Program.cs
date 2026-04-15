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
            var type = assembly.GetType("EnemyType");
            if (type == null)
            {
                Console.WriteLine("Type Not Found");
                return;
            }

            Console.WriteLine("--- Fields in EnemyType ---");
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                try {
                    Console.WriteLine($"{f.Name} (Type: {f.FieldType.Name})");
                } catch (Exception ex) {
                    Console.WriteLine($"{f.Name} (Error reading type: {ex.Message})");
                }
            }
            
            Console.WriteLine("--- Properties in EnemyType ---");
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                try {
                    Console.WriteLine($"{p.Name} (Type: {p.PropertyType.Name})");
                } catch (Exception ex) {
                    Console.WriteLine($"{p.Name} (Error reading type: {ex.Message})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
