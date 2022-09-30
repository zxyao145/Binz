using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    /// <summary>
    /// more see Zack.Commons
    /// </summary>
    public class ReflectionHelper
    {
        private class AssemblyEquality : EqualityComparer<Assembly>
        {
            public override bool Equals(Assembly? x, Assembly? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return AssemblyName.ReferenceMatchesDefinition(x!.GetName(), y!.GetName());
            }

            public override int GetHashCode([DisallowNull] Assembly obj)
            {
                return obj.GetName().FullName.GetHashCode();
            }
        }

        private static bool IsSystemAssembly(Assembly asm)
        {
            return asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company.Contains("Microsoft") ?? false;
        }

        private static bool IsManagedAssembly(string file)
        {
            using FileStream peStream = File.OpenRead(file);
            using PEReader pEReader = new PEReader((Stream)peStream);
            return pEReader.PEHeaders.CorHeader != null;
        }

        public static IEnumerable<Assembly> GetAllReferencedAssemblies(bool skipSystemAssemblies = true)
        {
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            HashSet<Assembly> hashSet = new HashSet<Assembly>(new AssemblyEquality());
            HashSet<string> hashSet2 = new HashSet<string>();
            Queue<Assembly> queue = new Queue<Assembly>();
            queue.Enqueue(assembly);
            if (skipSystemAssemblies && IsSystemAssembly(assembly))
            {
                hashSet.Add(assembly);
            }

            while (queue.Any())
            {
                AssemblyName[] referencedAssemblies = queue.Dequeue().GetReferencedAssemblies();
                foreach (AssemblyName assemblyName in referencedAssemblies)
                {
                    if (!hashSet2.Contains(assemblyName.FullName))
                    {
                        Assembly assembly2 = Assembly.Load(assemblyName);
                        if (!skipSystemAssemblies || !IsSystemAssembly(assembly2))
                        {
                            queue.Enqueue(assembly2);
                            hashSet2.Add(assemblyName.FullName);
                            hashSet.Add(assembly2);
                        }
                    }
                }
            }

            foreach (string item in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", new EnumerationOptions
            {
                RecurseSubdirectories = true
            }))
            {
                if (!IsManagedAssembly(item))
                {
                    continue;
                }

                AssemblyName asmName = AssemblyName.GetAssemblyName(item);
                if (!hashSet.Any((Assembly x) => AssemblyName.ReferenceMatchesDefinition(x.GetName(), asmName)))
                {
                    Assembly assembly3 = Assembly.Load(asmName);
                    if (!skipSystemAssemblies || !IsSystemAssembly(assembly3))
                    {
                        hashSet.Add(assembly3);
                    }
                }
            }

            return hashSet.ToArray();
        }
    }
}
