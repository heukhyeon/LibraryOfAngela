using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    public class EntryObserver : MonoBehaviour
    {
        public static EntryObserver Instance;
        public static void Create()
        {
            var obj = FindObjectOfType<EntryScene>();
            var observer = obj.gameObject.AddComponent<EntryObserver>();
            observer.scene = obj;
            Instance = observer;
            observer.activatedMods = obj.modPopup.dataList.Where(d => d?.IsActivated == true &&
        File.Exists(Path.Combine(d.ModInfo.dirInfo.FullName, "Assemblies", "LoALoader.dll")))
                    .Select(d => d.ModInfo)
                    .ToList();
        }

        public EntryScene scene;
        public List<ModContentInfo> activatedMods;
        private bool flag = false;
        void Update()
        {
            // 한국 연령 알림 나오면 강제로 꺼버림
            if (!flag && scene.ob_deliberationAlarmKr.activeSelf)
            {
                flag = true;
                scene.EndDeliberationAlarmInAnim();
            }
        }
    }
}
