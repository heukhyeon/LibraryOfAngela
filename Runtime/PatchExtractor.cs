using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LibraryOfAngela
{
    public class PatchExtractor
    {
        private const BindingFlags AllInstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private static readonly char[] PathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static void Run()
        {
            // 현재 어플리케이션에 적용된 모든 하모니 패치 메소드를 아래의 형태로 정리한다.
            // (메소드 이름. Full Name)
            // ㄴ (Prefix / Postfix / Transpiler / Finalizer 중 1개, 실제 확인) - (모드 이름) : (dll 이름) : (모드 경로)
            // ㄴ (이하 동일)
            // (다음 메소드 이름. Full Name)
            var logger = new StringBuilder("테스트 빌더\n");
            try
            {
                var modInfos = BuildModInfos();

                foreach (var original in Harmony.GetAllPatchedMethods().Distinct().OrderBy(GetMethodFullName))
                {
                    var patchInfo = Harmony.GetPatchInfo(original);
                    if (patchInfo == null) continue;

                    var lines = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    AddPatchLines(lines, modInfos, "Prefix", GetPatchCollection(patchInfo, "Prefixes"));
                    AddPatchLines(lines, modInfos, "Postfix", GetPatchCollection(patchInfo, "Postfixes"));
                    AddPatchLines(lines, modInfos, "Transpiler", GetPatchCollection(patchInfo, "Transpilers"));
                    AddPatchLines(lines, modInfos, "Finalizer", GetPatchCollection(patchInfo, "Finalizers"));

                    if (lines.Count == 0) continue;

                    logger.AppendLine(GetMethodFullName(original));
                    foreach (var d in lines) logger.AppendLine(d);
                }
                logger.AppendLine("분석 완료");
                Console.WriteLine(logger.ToString());
            }
            catch (Exception e)
            {
                logger.AppendLine($"에러 발생");
                Console.WriteLine(logger.ToString());
                Debug.LogError(e);
            }
        }

        private static void AddPatchLines(SortedSet<string> lines, ModInfoCache modInfos, string patchType, IEnumerable patches)
        {
            if (patches == null) return;

            foreach (var patch in patches)
            {
                var method = GetPatchMethod(patch);
                if (method == null) continue;

                var assembly = method.DeclaringType?.Assembly ?? method.Module?.Assembly;
                var owner = GetString(ReadValue(patch, "owner")) ?? GetString(ReadValue(patch, "Owner"));
                var info = modInfos.Resolve(assembly, owner);

                lines.Add($"ㄴ {patchType} - {info.ModName} : {info.DllName} : {info.ModPath}");
            }
        }

        private static IEnumerable GetPatchCollection(object patchInfo, string propertyName)
        {
            var value = ReadValue(patchInfo, propertyName);
            if (value == null)
            {
                value = ReadValue(patchInfo, char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1));
            }

            if (value is string) return null;
            return value as IEnumerable;
        }

        private static MethodInfo GetPatchMethod(object patch)
        {
            if (patch is MethodInfo method) return method;

            return ReadValue(patch, "PatchMethod") as MethodInfo
                ?? ReadValue(patch, "patchMethod") as MethodInfo
                ?? ReadValue(patch, "method") as MethodInfo
                ?? ReadValue(patch, "Method") as MethodInfo;
        }

        private static string GetMethodFullName(MethodBase method)
        {
            if (method == null) return "(Unknown)";

            var typeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "(Unknown)";
            string parameterText;

            try
            {
                parameterText = string.Join(", ", method.GetParameters().Select(x => x.ParameterType.FullName ?? x.ParameterType.Name).ToArray());
            }
            catch
            {
                parameterText = "";
            }

            return $"{typeName}.{method.Name}({parameterText})";
        }

        private static ModInfoCache BuildModInfos()
        {
            var cache = new ModInfoCache();

            CollectModContentInfos(cache);
            CollectAssemblyManagerInfos(cache);
            cache.ResolveAppDomainAssemblies();

            return cache;
        }

        private static void CollectModContentInfos(ModInfoCache cache)
        {
            try
            {
                var managerType = FindLoadedType("Mod.ModContentManager");
                var manager = ReadValue(managerType, "Instance");
                var mods = InvokeValue(manager, "GetAllMods") as IEnumerable;
                if (mods == null) return;

                foreach (var mod in mods)
                {
                    var path = GetDirectoryPath(ReadValue(mod, "dirInfo"));
                    var invInfo = ReadValue(mod, "invInfo");
                    var workshopInfo = ReadValue(invInfo, "workshopInfo");
                    var name = FirstNotEmpty(
                        GetString(ReadValue(workshopInfo, "title")),
                        GetString(ReadValue(invInfo, "title")),
                        GetString(ReadValue(workshopInfo, "uniqueId")),
                        GetString(ReadValue(invInfo, "uniqueId")),
                        GetFileName(path));

                    if (!string.IsNullOrEmpty(path))
                    {
                        cache.AddModRoot(path, name);
                    }
                }
            }
            catch
            {
            }
        }

        private static void CollectAssemblyManagerInfos(ModInfoCache cache)
        {
            try
            {
                var managerType = FindLoadedType("AssemblyManager");
                var manager = ReadValue(managerType, "Instance");
                var assemblyDict = ReadValue(manager, "_assemblyDict");
                if (assemblyDict == null) return;

                foreach (var entry in ReadDictionary(assemblyDict))
                {
                    var modName = entry.Key?.ToString();
                    var assemblies = entry.Value as IEnumerable;
                    if (assemblies == null) continue;

                    foreach (var value in assemblies)
                    {
                        if (value is Assembly assembly)
                        {
                            cache.AddAssembly(assembly, modName, GetAssemblyDirectory(assembly));
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static IEnumerable<DictionaryEntry> ReadDictionary(object dictionary)
        {
            if (dictionary is IDictionary typed)
            {
                foreach (DictionaryEntry entry in typed)
                {
                    yield return entry;
                }

                yield break;
            }

            if (!(dictionary is IEnumerable enumerable)) yield break;

            foreach (var entry in enumerable)
            {
                yield return new DictionaryEntry(ReadValue(entry, "Key"), ReadValue(entry, "Value"));
            }
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = null;
                try
                {
                    type = assembly.GetType(fullName, false);
                }
                catch
                {
                }

                if (type != null) return type;
            }

            return null;
        }

        private static object InvokeValue(object target, string name)
        {
            if (target == null) return null;

            var type = target as Type ?? target.GetType();
            var flags = target is Type ? AllStaticFlags : AllInstanceFlags;

            for (var current = type; current != null; current = current.BaseType)
            {
                var method = current.GetMethods(flags).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.GetParameters().Length == 0);
                if (method == null) continue;

                try
                {
                    return method.Invoke(target is Type ? null : target, null);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static object ReadValue(object target, string name)
        {
            if (target == null) return null;

            var type = target as Type ?? target.GetType();
            var flags = target is Type ? AllStaticFlags : AllInstanceFlags;

            for (var current = type; current != null; current = current.BaseType)
            {
                var property = current.GetProperties(flags).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.GetIndexParameters().Length == 0);
                if (property != null)
                {
                    try
                    {
                        return property.GetValue(target is Type ? null : target, null);
                    }
                    catch
                    {
                        return null;
                    }
                }

                var field = current.GetFields(flags).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (field != null)
                {
                    try
                    {
                        return field.GetValue(target is Type ? null : target);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        private static string GetDirectoryPath(object value)
        {
            if (value is DirectoryInfo directory) return directory.FullName;
            return GetString(ReadValue(value, "FullName")) ?? GetString(value);
        }

        private static string GetAssemblyDirectory(Assembly assembly)
        {
            var location = GetAssemblyLocation(assembly);
            if (string.IsNullOrEmpty(location)) return null;

            try
            {
                return Path.GetDirectoryName(location);
            }
            catch
            {
                return null;
            }
        }

        private static string GetAssemblyLocation(Assembly assembly)
        {
            try
            {
                return assembly?.Location;
            }
            catch
            {
                return null;
            }
        }

        private static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                return Path.GetFileName(path.TrimEnd(PathSeparators));
            }
            catch
            {
                return null;
            }
        }

        private static string GetString(object value)
        {
            return value == null ? null : value.ToString();
        }

        private static string FirstNotEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value)) return value;
            }

            return null;
        }

        private sealed class ModInfoCache
        {
            private readonly Dictionary<Assembly, PatchModInfo> assemblyInfos = new Dictionary<Assembly, PatchModInfo>();
            private readonly List<PatchModInfo> roots = new List<PatchModInfo>();

            public void AddModRoot(string path, string modName)
            {
                var fullPath = NormalizePath(path);
                if (string.IsNullOrEmpty(fullPath)) return;

                if (roots.Any(x => string.Equals(x.ModPath, fullPath, StringComparison.OrdinalIgnoreCase))) return;

                roots.Add(new PatchModInfo
                {
                    ModName = FirstNotEmpty(modName, GetFileName(fullPath)),
                    ModPath = fullPath
                });
            }

            public void AddAssembly(Assembly assembly, string modName, string modPath)
            {
                if (assembly == null) return;

                var current = ResolveByRoot(assembly);
                var info = new PatchModInfo
                {
                    ModName = FirstNotEmpty(current?.ModName, modName),
                    DllName = GetAssemblyDllName(assembly),
                    ModPath = FirstNotEmpty(current?.ModPath, NormalizePath(modPath))
                };

                assemblyInfos[assembly] = info;
            }

            public void ResolveAppDomainAssemblies()
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assemblyInfos.ContainsKey(assembly)) continue;

                    var info = ResolveByRoot(assembly);
                    if (info == null) continue;

                    assemblyInfos[assembly] = new PatchModInfo
                    {
                        ModName = info.ModName,
                        DllName = GetAssemblyDllName(assembly),
                        ModPath = info.ModPath
                    };
                }
            }

            public PatchModInfo Resolve(Assembly assembly, string owner)
            {
                PatchModInfo info = null;
                if (assembly != null && !assemblyInfos.TryGetValue(assembly, out info))
                {
                    info = ResolveByRoot(assembly);
                }

                return new PatchModInfo
                {
                    ModName = FirstNotEmpty(info?.ModName, owner, assembly?.GetName().Name, "(Unknown)"),
                    DllName = FirstNotEmpty(info?.DllName, GetAssemblyDllName(assembly), "(Unknown)"),
                    ModPath = FirstNotEmpty(info?.ModPath, NormalizePath(GetAssemblyDirectory(assembly)), "(Unknown)")
                };
            }

            private PatchModInfo ResolveByRoot(Assembly assembly)
            {
                var location = NormalizePath(GetAssemblyLocation(assembly));
                if (string.IsNullOrEmpty(location)) return null;

                return roots
                    .OrderByDescending(x => x.ModPath?.Length ?? 0)
                    .FirstOrDefault(x => IsSameOrChildPath(location, x.ModPath));
            }

            private static string GetAssemblyDllName(Assembly assembly)
            {
                var location = GetAssemblyLocation(assembly);
                if (string.IsNullOrEmpty(location)) return null;

                try
                {
                    return Path.GetFileName(location);
                }
                catch
                {
                    return null;
                }
            }
        }

        private sealed class PatchModInfo
        {
            public string ModName;
            public string DllName;
            public string ModPath;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                return Path.GetFullPath(path).TrimEnd(PathSeparators);
            }
            catch
            {
                return path.TrimEnd(PathSeparators);
            }
        }

        private static bool IsSameOrChildPath(string path, string root)
        {
            path = NormalizePath(path);
            root = NormalizePath(root);

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root)) return false;
            if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase)) return true;

            return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
