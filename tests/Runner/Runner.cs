using System;
using System.IO;
using System.Reflection;

namespace UnfairBalance.Runner
{
    static class Program
    {
        static int Main()
        {
            var managed = @"D:\SteamLibrary\steamapps\common\Pathfinder Second Adventure\Wrath_Data\Managed";
            var umm = Path.Combine(managed, "UnityModManager");
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var file = new AssemblyName(args.Name).Name + ".dll";
                var direct = Path.Combine(managed, file);
                if (File.Exists(direct))
                {
                    return Assembly.LoadFrom(direct);
                }

                var modManager = Path.Combine(umm, file);
                if (File.Exists(modManager))
                {
                    return Assembly.LoadFrom(modManager);
                }

                return null;
            };

            var testsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnfairBalance.Tests.dll");
            var tests = Assembly.LoadFrom(testsPath);
            var entry = tests.GetType("UnfairBalance.Tests.Program");
            return (int)entry.GetMethod("Run").Invoke(null, null);
        }
    }
}
