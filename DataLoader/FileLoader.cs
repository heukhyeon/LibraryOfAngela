using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Workshop;

namespace LoADataLoader
{
    internal enum FileType
    {
        DATA,
        SKIN,
        CARDWORK
    }

    internal class FileLoader
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
            }
            return Task.WhenAll(tasks);
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
