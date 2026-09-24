using Mod;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LoALoader
{
    internal static class FileLoader
    {
        internal static Task CreateDllTask(List<ModContentInfo> mods)
        {
            return Task.Run(() =>
            {
                foreach (var mod in mods)
                {
                    var dirPath = Path.Combine(mod.dirInfo.FullName, "Assemblies", "LazyDll");
                    var modName = mod.invInfo.workshopInfo.title;
                    var packageId = mod.invInfo.workshopInfo.uniqueId;

                    foreach (var file in Directory.GetFiles(dirPath, "*.dll", SearchOption.AllDirectories))
                    {
                        var name = Path.GetFileName(file);
                        // LoALoader is the bootstrap assembly that is already loaded from Assemblies.
                        // A different version copied under LazyDll must not be forwarded to Runtime,
                        // otherwise LoAModCache creates its ModInitializer a second time.
                        if (name == "LoALoader.dll" || name == "LoAInterface.dll" || name == "LoARuntimeUI.dll" || name == "LoADataLoader.dll") continue;
                        if (LoAInitializer.whiteListDll.Contains(name)) continue;
                        if (name == "LoARuntime.dll")
                        {
                            LoAInitializer.Instance.observer.CompareRuntimeDllVersion(modName, name, file);
                        }
                        else
                        {
                            LoAInitializer.Instance.observer.AddAssembly(packageId, modName, file);
                        }
                    }
                }
            });
        }
    }
}
