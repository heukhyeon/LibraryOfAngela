using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workshop;

namespace LoALoader
{
    internal enum FileType
    {
        DATA,
        SKIN,
        CARDWORK,
        INIT
    }

    public class FileLoader
    {
        internal static Task LoadAll(string packageId, string modName, string dirPath, FileType type, bool onlyTopDirectory)
        {
            var tasks = new List<Task>();
            switch (type)
            {
                case FileType.DATA:
                    var option = onlyTopDirectory ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                    foreach (var file in Directory.GetFiles(dirPath, "*.xml", option))
                    {
                        tasks.Add(Load(packageId, modName, file, type));
                    }
                    break;
                case FileType.SKIN:
                    // DIR의 바로 아래 자식 디렉토리만 순회
                    var subDirectories = Directory.GetDirectories(dirPath);
                    foreach (var subDir in subDirectories)
                    {
                        var modInfoPath = Path.Combine(subDir, "ModInfo.Xml");
                        if (File.Exists(modInfoPath))
                        {
                            tasks.Add(Load(packageId, modName, modInfoPath, type));
                        }
                    }
                    break;
                case FileType.CARDWORK:
                    return Task.Run(() =>
                    {
                        var list = new List<ArtworkCustomizeData>();
                        foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", onlyTopDirectory ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
                        {
                            var name = Path.GetFileName(file);
                            if (name.EndsWith(".png") || name.EndsWith(".jpg"))
                            {
                                list.Add(new ArtworkCustomizeData { spritePath = file, name = name });
                            }
                        }
                        FileParser.cardworks.Enqueue(new CardWorkParse { packageId = packageId, datas = list });
                    });
                case FileType.INIT:
                    return FileParser.Enqueue(packageId, modName, dirPath, null, type);
            }
            return Task.WhenAll(tasks);
        }

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
                        if (name == "LoAInterface.dll" || name == "LoARuntimeUI.dll") continue;
                        if (LoAInitializer.whiteListDll.Contains(name)) continue;
                        if (name == "LoARuntime.dll")
                        {
                            LoAInitializer.Instance.observer.CompareRuntimeDllVersion(modName, name, file);
                        }
                        else
                        {
                            FileParser.assembliePaths.Enqueue(new FileParseRequest { packageId = packageId, modName = modName, path = file });
                        }
                    }
                }
            });
        }

        public static Task LoadData(string packageId, string dirPath)
        {
            return Task.Run(async () =>
            {
                var tasks = new List<Task>();
                foreach (var file in Directory.GetFiles(dirPath, "*.xml", SearchOption.AllDirectories))
                {
                    tasks.Add(Load(packageId, "", file, FileType.DATA));
                }
                await Task.WhenAll(tasks);
            });
        }

        static Task Load(string packageId, string modName, string path, FileType type)
        {
            return Task.Run(async () =>
            {
                // FileStream을 비동기 모드로 열기
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                {
                    // MemoryStream을 사용하여 파일 내용을 메모리에 저장
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream);
                        var bytes = memoryStream.ToArray();
                        await FileParser.Enqueue(packageId, modName, path, bytes, type);
                    }
                }
            });
        }

    }
}
