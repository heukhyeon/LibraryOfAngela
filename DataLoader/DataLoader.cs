using Mod;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LoADataLoader
{
    public static class DataLoader
    {
        private static readonly object initializationLock = new object();
        private static Task initialLoadTask;

        public static void Initialize(List<ModContentInfo> activatedMods, string currentPackageId)
        {
            lock (initializationLock)
            {
                if (initialLoadTask != null) return;

                var mods = activatedMods?.ToList() ?? new List<ModContentInfo>();
                initialLoadTask = Task.Run(async () =>
                {
                    var tasks = new List<Task>();
                    foreach (var mod in mods)
                    {
                        var dir = mod.dirInfo.FullName;
                        var packageId = mod.invInfo.workshopInfo.uniqueId;
                        var modName = mod.invInfo.workshopInfo.title;

                        if (packageId == currentPackageId)
                        {
                            tasks.Add(FileLoader.LoadAll(
                                packageId,
                                modName,
                                Path.Combine(dir, "Resource", "CharacterSkin"),
                                FileType.SKIN,
                                false));
                        }
                        else
                        {
                            tasks.Add(FileLoader.LoadAll(
                                packageId,
                                modName,
                                Path.Combine(dir, "Data"),
                                FileType.DATA,
                                true));
                            tasks.Add(FileLoader.LoadAll(
                                packageId,
                                modName,
                                Path.Combine(dir, "Resource", "CharacterSkin"),
                                FileType.SKIN,
                                true));
                            tasks.Add(FileLoader.LoadAll(
                                packageId,
                                modName,
                                Path.Combine(dir, "Resource", "CombatPageArtwork"),
                                FileType.CARDWORK,
                                false));
                        }
                    }

                    await Task.WhenAll(tasks);
                    FileParser.InitCheckSkip();
                });
            }
        }

        public static Task WaitInitialLoadComplete()
        {
            lock (initializationLock)
            {
                return initialLoadTask ?? Task.FromException(
                    new System.InvalidOperationException("LoADataLoader.Initialize must be called before the runtime starts."));
            }
        }

        public static void CallInitializerComplete()
        {
            FileParser.CallInitializerComplete();
        }

        public static Task WaitCallInitializerComplete()
        {
            return FileParser.WaitCallInitializerComplete();
        }

        public static Task LoadData(string packageId, string dirPath)
        {
            return FileLoader.LoadData(packageId, dirPath);
        }

        public static Task WaitDataComplete()
        {
            return FileParser.WaitDataComplete();
        }

        public static Task WaitCardWorkComplete()
        {
            return FileParser.WaitCardWorkComplete();
        }
    }
}
