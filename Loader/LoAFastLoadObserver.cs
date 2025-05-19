using Mod;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    class LoAFastLoadObserver
    {
        struct DllRecord
        {
            public string modName;
            public string path;
            public string name;
            public uint ver1;
            public uint ver2;
            public uint ver3;
            public uint ver4;
            public bool runtime;
        }

        struct DllLoadRecord
        {
            public string modName;
            public long startTime;
            public long endTime;
        }

        private DllRecord currentLoARuntimeRecord;
        private Type runtimeType;
        private string runtimeDir;
        private long createdTime;
        private long callInitializerTime;
        private long callInitializerCompleteTime;
        private long saveSelectionDataCompleteTime;
        private long runtimeDllLoadTime;
        private long initTaskCompleteTime;
        private long dllTaskCompleteTime;
        private long enqueueDuration;
        private Queue<DllLoadRecord> loadRecords = new Queue<DllLoadRecord>();

        public List<Task> initTasks = new List<Task>();
        public Task dllTask;
        

        public void Init()
        {
            createdTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var t = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var builder = new StringBuilder("Detected LoA Mods\n");

            foreach (var mod in LoAInitializer.Instance.activatedMods)
            {
                var dir = mod.dirInfo.FullName;
                builder.AppendLine($"- {mod.invInfo.workshopInfo.title} // {dir}");
                
                //TODO 스킨은 클래스를 커스텀하므로 다시 만들어야할수도 있음.
                if (mod.invInfo.workshopInfo.uniqueId == ModContentManager.Instance._currentPid)
                {
                    FileLoader.LoadAll(mod.invInfo.workshopInfo.uniqueId, mod.invInfo.workshopInfo.title, Path.Combine(dir, "Resource\\CharacterSkin"), FileType.SKIN, false);
                }
                else
                {
                    initTasks.Add(FileLoader.LoadAll(mod.invInfo.workshopInfo.uniqueId, mod.invInfo.workshopInfo.title, dir, FileType.INIT, true));
                }
            }
            dllTask = FileLoader.CreateDllTask(LoAInitializer.Instance.activatedMods);
            enqueueDuration += (DateTimeOffset.Now.ToUnixTimeMilliseconds() - t);
            UnityEngine.Debug.Log(builder.ToString());
        }

        public void LoadCommonAssemblies(ModContent mod)
        {
            var t = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            mod.LoadAssemblies();
            loadRecords.Enqueue(new DllLoadRecord
            {
                modName = mod._modInfo.invInfo.workshopInfo.title,
                startTime = t,
                endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            });
        }

        public void IsCalledInitializer()
        {
            callInitializerTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //Debug.Log("Called Initializer 1");
            // var loaderMod = ModContentManager.Instance._loadedContents[expectedLoaderModIndex]._modInfo;
            Task.Run(async () =>
            {
                //Debug.Log("Called Initializer 2");
                await dllTask;
                //Debug.Log("Called Initializer 3");
                dllTaskCompleteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var assemblies = LoadRuntimeDlls();
                //Debug.Log("Called Initializer 4");
                while (true)
                {
                    bool flag = false;
                    for (int i = 0; i < initTasks.Count; i++)
                    {
                        if (!initTasks[i].IsCompleted)
                        {
                            Debug.Log($"Called Initializer 4 - 1 : {i} // {initTasks.Count}");
                            flag = true;
                        }
                    }
                    if (!flag) break;  
                }
                //Debug.Log("Called Initializer 5");
                FileParser.InitCheckSkip();
                initTaskCompleteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                End(assemblies);
                //Debug.Log("Called Initializer 6");
                initTasks.Clear();
            });
        }

        public void CallInitializerComplete()
        {
            //Debug.Log("Called Initializer Final");
            callInitializerCompleteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            FileParser.CallInitializerComplete();
        }

        public void SaveSelectionDataComplete()
        {
            saveSelectionDataCompleteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public void CompareRuntimeDllVersion(string modName, string name, string path)
        {
            var version = FileSearcher.GetDllVersion(path);
            var legacy = currentLoARuntimeRecord;
            int dir = 0;
            if (legacy.ver1 != version.Item1)
            {
                dir = legacy.ver1 > version.Item1 ? -1 : 1;
            }
            if (dir == 0 && legacy.ver2 != version.Item2)
            {
                dir = legacy.ver2 > version.Item2 ? -1 : 1;
            }
            if (dir == 0 && legacy.ver3 != version.Item3)
            {
                dir = legacy.ver3 > version.Item3 ? -1 : 1;
            }
            if (dir == 0 && legacy.ver4 != version.Item4)
            {
                dir = legacy.ver4 > version.Item4 ? -1 : 1;
            }

            if (dir == 1)
            {
                LoaderLogger.AppendLine($"--> Update Runtime Latest : {legacy.modName} - {modName}");
                currentLoARuntimeRecord = new DllRecord { modName = modName, name = name, path = path, ver1 = version.Item1, ver2 = version.Item2, ver3 = version.Item3, ver4 = version.Item4, runtime = true };
            }
        }

        private List<Assembly> LoadRuntimeDlls()
        {
            runtimeDllLoadTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var record = currentLoARuntimeRecord;
            var asm = Assembly.LoadFile(record.path);
            var assemblies = new List<Assembly>();
            assemblies.Add(asm);
            runtimeType = asm.GetType("LibraryOfAngela.LoAFramework");
            runtimeDir = Path.GetDirectoryName(record.path);
            var interfacePath = Path.Combine(runtimeDir, "LoAInterface.dll");
            var interfaceAsm = Assembly.LoadFile(interfacePath);
            var uiAsm = Assembly.LoadFile(Path.Combine(runtimeDir, "LoARuntimeUI.dll"));
            LoaderLogger.AppendLine("Load Dll");
            foreach (var r in FileParser.assembliePaths)
            {
                LoaderLogger.AppendLine($"- in {r.modName} : {r.path}");
                assemblies.Add(Assembly.LoadFile(r.path));
            }
            LoaderLogger.AppendLine();
            LoaderLogger.AppendLine($"LoARuntime Version : {record.ver1}.{record.ver2}.{record.ver3}.{record.ver4} in {record.modName} ({record.path})");
            LoaderLogger.AppendLine($"---- Interface : {interfaceAsm.GetName().Version} Path 1 : ({interfaceAsm.Location}) Path 2 : ({interfacePath})");
            LoaderLogger.AppendLine($"---- RuntimeUI : {uiAsm.GetName().Version}");
            LoaderLogger.AppendLine();
            return assemblies;
        }

        private void End(List<Assembly> assemblies)
        {
            if (runtimeType != null)
            {
                var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                LoaderLogger.AppendLine($"LoA Loader :: Init Complete");
                LoggingDuration("Total Duration", createdTime, now);
                LoggingDuration("Created ~ CallInitializer", createdTime, callInitializerTime);
                LoaderLogger.AppendLine($"(Enqueue Process Duration : {enqueueDuration / 1000.0}s)");
                var total = 0.0;
                LoaderLogger.AppendLine("Dll Load Time");
                foreach (var record in loadRecords)
                {
                    var duration = (record.endTime - record.startTime) / 1000.0;
                    total += duration;
                    LoaderLogger.AppendLine($"- {record.modName} : {duration}s");
                }
                LoaderLogger.AppendLine($"(Dll Load Time Total : {total})");
                LoggingDuration("CallInitializer Duration", callInitializerTime, callInitializerCompleteTime);
                LoggingDuration("SaveSelectionData Duration", callInitializerCompleteTime, saveSelectionDataCompleteTime);
                LoggingDuration("SaveSelectionData ~ Init Task Complete", saveSelectionDataCompleteTime, initTaskCompleteTime);
                LoggingDuration("Init Task Complete ~ Dll Task Complete", initTaskCompleteTime, dllTaskCompleteTime);
                LoggingDuration("Dll Task Complete ~ Runtime Dll Load", dllTaskCompleteTime, runtimeDllLoadTime);
                LoggingDuration("Runtime Dll Load ~ End", runtimeDllLoadTime, now);

                var latestLoAAsset = Path.Combine(runtimeDir, "LoAAsset");
                LoaderLogger.Flush();
                runtimeType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { assemblies });
                runtimeType.GetMethod("InjectAsset", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { latestLoAAsset });
            }
            else
            {
                var assm = Assembly.GetExecutingAssembly();
                var myModDir = new DirectoryInfo(Path.GetDirectoryName(assm.Location)).Parent;
                var myMod = ModContentManager.Instance.GetAllMods().Find(d => d.dirInfo == myModDir);
                var myModName = myMod?.invInfo.workshopInfo.title ?? myModDir.FullName;

                UnityEngine.Debug.Log($"LoA Loader :: LoA Runtime Not Found, Please Check From {myModName}");
                //ModContentManager.Instance.AddErrorLog($"LoA :: LoA Runtime Not Found, Please Check From {myModName}");
            }
        }

        private void LoggingDuration(string title, long before, long after)
        {
            var elepsed = after - before;
            if (before == 0L || elepsed <= 0L)
            {
                LoaderLogger.AppendLine(string.Format("- {0} : Skipped", title));
            }
            else
            {
                LoaderLogger.AppendLine(string.Format("- {0} : {1}s", title, elepsed / 1000.0));
            }
        }

    }
}
