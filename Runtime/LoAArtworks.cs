using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    class LoAArtworks : Singleton<LoAArtworks>, ILoAArtworkGetter
    {
        private HashSet<AssetBundleInfo> duplicateInfoCheckSet = new HashSet<AssetBundleInfo>();
        private List<AssetBundleArtworkInfo> reservedInfos = new List<AssetBundleArtworkInfo>();

        private List<ArtworkTarget> targets = new List<ArtworkTarget>();
        private Dictionary<ArtworkKey, ArtworkTarget> fastTargets = new Dictionary<ArtworkKey, ArtworkTarget>();

        public Task Initialize()
        {
            return Task.Run(() =>
            {
                foreach (var mod in LoAModCache.Instance.Select(x => x.ArtworkConfig))
                {
                    try
                    {
                        mod.GetArtworkDatas()?.ForEach(x =>
                        {
                            x.packageId = mod.packageId;

                            if (x is FileArtworkInfo i)
                            {
                                var p = PathProvider.ConvertValidPath(mod.packageId, i.path);

                                foreach (var file in Directory.GetFiles(p, "*.png", SearchOption.AllDirectories))
                                {
                                    InjectTarget(new ArtworkTarget
                                    {
                                        isLoaded = false,
                                        packageId = mod.packageId,
                                        path = file,
                                        name = Path.GetFileNameWithoutExtension(file)
                                    });
                                }
                            }
                            else if (x is AssetBundleArtworkInfo art)
                            {
                                reservedInfos.Add(art);
                            }
                        });
                    }
                    catch (NotImplementedException)
                    {
                        Logger.Log($"{mod.packageId} is Not Implmeneted LoAArtwork. Skip");
                    }
                }
            });
        }

        bool ILoAArtworkGetter.ContainsKey(string packageId, string name)
        {
            return fastTargets.ContainsKey(new ArtworkKey { packageId = packageId, name = name.ToLower() });
        }

        public Sprite GetSprite(string packageId, string name, bool showWarning)
        {
            var target = fastTargets.SafeGet(new ArtworkKey { packageId = packageId, name = name.ToLower() });
            if (target == null && showWarning) {
                var stringBuilder = new StringBuilder($"Requested {name} (from {packageId}) Sprite But Not Found Target, Please Check Target Data");
                stringBuilder.AppendLine(Environment.StackTrace);
                Logger.Log(stringBuilder.ToString());
            }
            return target?.Load();
        }

        public void OnAssetBundleLoaded(AssetBundleInfo info, string packageName, AssetBundle bundle)
        {
            try
            {
                if (bundle is null) return;

                if (duplicateInfoCheckSet.Contains(info))
                {
                    foreach (var target in targets.Where(x => x.matchedInfo == info))
                    {
                        target.bundle = bundle;
                    }
                    return;
                }
                duplicateInfoCheckSet.Add(info);

                int i = 0;
                bool injectFlag = false;
                var bundleTargets = bundle.GetAllAssetNames();
                var logger = new StringBuilder($"LoA Artwork AssetBundle Notified : {info.path}\n");
                while (i < reservedInfos.Count)
                {
                    var target = reservedInfos[i];
                    if (target.packageId == packageName)
                    {
                        foreach (var asset in bundleTargets.Where(x => x.StartsWith(target.path) && x.EndsWith(".png")))
                        {
                            injectFlag = true;
                            var key = Path.GetFileNameWithoutExtension(asset);
                            logger.AppendLine($"- {asset}");

                            InjectTarget(new ArtworkTarget
                            {
                                isLoaded = false,
                                path = asset,
                                name = key,
                                matchedInfo = info,
                                bundle = bundle,
                                packageId = packageName,
                                sprite = null
                            });
                        }
                    }
                    i++;
                }
                if (injectFlag && LoAFramework.DEBUG) Logger.Log(logger.ToString());
            }
            catch (Exception e)
            {
                Logger.Log($"Artwork Load Fail in {packageName} :: {info.path}");
                Logger.LogError(e);
            }

        }

        public void OnAssetBundleUnloaded(AssetBundleInfo info)
        {
            targets.ForEach(x =>
            {
                if (x.matchedInfo == info)
                {
                    x.isLoaded = false;
                    x.bundle = null;
                    x.sprite = null;
                }
            });
            duplicateInfoCheckSet.Remove(info);
        }
    
        public Sprite CreateFileSprite(string path)
        {
            Texture2D texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(File.ReadAllBytes(path));
            return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
        }

        public void InputSprite(string packageId, string name, Sprite sp)
        {
            fastTargets[new ArtworkKey { packageId = packageId, name = name.ToLower() }] = new ArtworkTarget
            {
                packageId = packageId,
                name = name,
                isLoaded = true,
                sprite = sp,
            };
        }
    
        private void InjectTarget(ArtworkTarget target)
        {
            targets.Add(target);
            fastTargets[target.Key] = target;
        }
    }

    class ArtworkTarget
    {
        public ArtworkKey Key
        {
            get => new ArtworkKey { packageId = packageId, name = name.ToLower() };
        }

        public string packageId;
        public string name;
        public string path;
        public AssetBundleInfo matchedInfo;
        public AssetBundle bundle;
        public bool isLoaded;
        public Sprite sprite;

        public Sprite Load()
        {
            if (isLoaded) return sprite;
            if (matchedInfo != null)
            {
                sprite = bundle?.LoadAsset<Sprite>(path);
                isLoaded = true;
                if (sprite == null) Logger.Log($"Sprite Path Valid But Not Found Sprite ({packageId} // {name}) -> {path}");
                return sprite;
            }
            sprite = LoAArtworks.Instance.CreateFileSprite(path);
            isLoaded = true;
            return sprite;
        }
    }

    struct ArtworkKey
    {
        public string packageId;
        public string name;
    }
}
