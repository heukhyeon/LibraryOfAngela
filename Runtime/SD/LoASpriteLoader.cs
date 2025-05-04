using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LibraryOfAngela.SD
{
    class LoASpriteLoader
    {
        struct ApplyTarget
        {
            public Texture2D texture;
            public RenderCacheData data;
        }

        public enum SkinType {
            Normal = 1,
            Front = 2,
            Skin = 4
        }

        public static bool isAsyncLoad;

        public static Sprite LoadDefaultMotion(RenderCacheData data)
        {
            if (data is null) return null;

            var sp = LoadRealSprite(data, SkinType.Normal);
            data.realSprite = sp;
            data.data._sprite = sp;
            return sp;
        }

        public static Sprite LoadDefaultMotion(string packageId, string skinName)
        {
            var data = LoASDTarget.skinSet.SafeGet(new SkinComponentKey { packageId = packageId, skinName = skinName })
                ?.Find(d => d.motion == ActionDetail.Default);
            return LoadDefaultMotion(data);
        }

        public static Sprite LoadRealSprite(RenderCacheData data, SkinType skinType)
        {
            string key = data.targetName;
            bool nullExpected = false;
            if (skinType.HasFlag(SkinType.Front)) {
                key += "_front";
                nullExpected = true;
            }
            if (skinType.HasFlag(SkinType.Skin)) {
                key += "_skin";
                nullExpected = true;
            }
            if (data.isAssetBundle)
            {
                return LoAModCache.Instance[data.packageId]
                    .AssetBundles
                    .LoadManullay<Sprite>(key, nullExpected);
            }
            else
            {
                if (LoAFramework.DEBUG)
                {
                    Logger.Log($"LoA File Real Load!!! {data.packageId} // {data.skinName} // {key}");
                }

                var sp = SpriteUtil.LoadLargePivotSprite(key, data.data.pivotPos);
                return sp;
            }
        }

        private static async Task<ApplyTarget> LoadFileSpriteDownload(RenderCacheData data)
        {
            if (LoAFramework.DEBUG)
            {
                Logger.Log($"LoA Sprite Async Load :: {data.targetName}");
            }

            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + data.targetName))
            {
                var req = www.SendWebRequest();
                Texture2D t = null;
                int retryCount = 0;
                while (true)
                {
                    await Task.Delay(60);
                    if (req.isDone)
                    {
                        if (!string.IsNullOrEmpty(www.error) && www.error.Contains("404"))
                        {
                            return new ApplyTarget { texture = null, data = data };
                        }
                        try
                        {
                            t = DownloadHandlerTexture.GetContent(req.webRequest);
                            break;
                        }
                        catch (InvalidOperationException e)
                        {
                            if (retryCount++ < 5)
                            {
                                if (LoAFramework.DEBUG)
                                {
                                    Logger.Log($"InvalidOperationException in {data.targetName}, Retry : {retryCount} // {www.error}");
                                }

                                await Task.Delay(RandomUtil.Range(100, 150));
                            }
                            else
                            {
                                throw e;
                            }
                        }

                    }
                }
                var texture = new Texture2D(t.width, t.height, TextureFormat.RGBA32, true);

                await Task.Run(() =>
                {
                    var p = texture.GetRawTextureData<Color32>();
                    var p2 = t.GetPixels();
                    var index = 0;
                    for (int i = 0; i < t.height; i++)
                    {
                        for (int j = 0; j < t.width; j++)
                        {
                            p[index] = p2[index];
                            index++;
                        }
                    }
                });

                return new ApplyTarget
                {
                    texture = texture,
                    data = data
                };
            }
        }

        public static async void LoadSpriteAsync()
        {
            isAsyncLoad = true;
            var queue = new LinkedList<Task<ApplyTarget>>();
            var requiredQueue = LoASDTarget.skinSet?.Values
                ?.SelectMany(d => d)
                ?.Where(x => x.realSprite == null && !x.isAssetBundle)
                ?.ToList() ?? new List<RenderCacheData>();
            while (true)
            {
                var skipState = 0;
                try
                {
                    var first = queue.FirstOrDefault();
                    if (first?.IsCompleted == true)
                    {
                        var item = first.Result;
                        if (item.texture != null)
                        {
                            item.texture.Apply();
                            await Task.Delay(120); ;
                            var ppu = LoAModCache.Instance[item.data.packageId]?.ArtworkConfig?.HandlePixelPerUnitFileArtwork(item.data.skinName,
                                Path.GetFileNameWithoutExtension(item.data.targetName), item.texture) ?? 50f;

                            var sp = Sprite.Create(item.texture, new Rect(0f, 0f, (float)item.texture.width, (float)item.texture.height), item.data.data.pivotPos, ppu, 0U, SpriteMeshType.FullRect);
                            item.data.realSprite = sp;
                            if (item.data.data.direction == CharacterMotion.MotionDirection.SideView)
                            {
                                item.data.data._frontSprite = sp;
                            }
                            else
                            {
                                item.data.data._sprite = sp;
                            }
                        }

                        queue.Remove(first);
                        await Task.Delay(60);
                        if (queue.Count > 30) continue;
                    }
                    if (queue.Count >= 60)
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                    var target = requiredQueue.FirstOrDefault();
                    if (target != null)
                    {
                        requiredQueue.RemoveAt(0);
                        queue.AddLast(LoadFileSpriteDownload(target));
                        skipState = 2;
                    }
                    else if (queue.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    skipState = -1;
                }
                if (skipState == -1)
                {
                    await Task.Delay(6000);
                }
            }
        }
    }
}
