using Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Buf
{
    class BufAssetLoader
    {
        private static BufAssetLoader Instance;
        private AssetBundle bundle;
        private Dictionary<string, string> paths;

        public BufAssetLoader(AssetBundle bundle)
        {
            Instance = this;
            this.bundle = bundle;
            paths = new Dictionary<string, string>();
            foreach (var asset in bundle.GetAllAssetNames())
            {
                var key = Path.GetFileNameWithoutExtension(asset).ToLower();
                paths[key] = asset;
            }
        }

        public static GameObject LoadObject(string name, Transform parent, float time)
        {
            var obj = Instance.bundle.LoadAsset<GameObject>(Instance.paths[name.ToLower()]);
            obj = UnityEngine.Object.Instantiate(obj, parent);
            if (time > 0f) obj.AddComponent<AutoDestruct>().time = time;
            return obj;
        }

        public static Sprite LoadImage(string name)
        {
            return Instance.bundle.LoadAsset<Sprite>(Instance.paths[name.ToLower()]);
        }

        public static void PlaySfx(string name)
        {
            SoundEffectManager.Instance.PlayClip(Instance.bundle.LoadAsset<AudioClip>(Instance.paths[name.ToLower()]));
        }
    }
}
